using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using Mono.Cecil;

// Conservative IL access preflight against unmodified installed assemblies. Findings are
// blockers, not a simulation of RoR2BepInExPack's runtime publicizer or a runtime pass.
internal static class AccessScan
{
    private static readonly HashSet<string> Unresolved = new HashSet<string>();
    private static readonly HashSet<string> Inaccessible = new HashSet<string>();
    private static readonly Dictionary<string, string> Assemblies = new Dictionary<string, string>();
    private static int checkedMembers;

    private static T Resolve<T>(Func<T> resolve, string name) where T : class
    {
        try
        {
            T value = resolve();
            if (value == null) Unresolved.Add(name + " : resolved null");
            return value;
        }
        catch (Exception e) { Unresolved.Add(name + " : " + e.GetType().Name + " " + e.Message); return null; }
    }

    private static bool Derived(TypeDefinition caller, TypeDefinition owner)
    {
        var seen = new HashSet<string>();
        for (var t = caller; t != null && seen.Add(t.FullName);)
        {
            if (t.FullName == owner.FullName && t.Module.Assembly.Name.Name == owner.Module.Assembly.Name.Name) return true;
            t = t.BaseType == null ? null : Resolve(() => t.BaseType.Resolve(), t.BaseType.FullName);
        }
        return false;
    }

    private static bool Visible(TypeDefinition t, TypeDefinition caller)
    {
        if (t.Module == caller.Module) return true;
        if (!t.IsNested) return t.IsPublic;
        return Visible(t.DeclaringType, caller) && (t.IsNestedPublic ||
            ((t.IsNestedFamily || t.IsNestedFamilyOrAssembly) && Derived(caller, t.DeclaringType)));
    }

    private static void Remember(ModuleDefinition module)
    {
        string path = module.FileName;
        if (!string.IsNullOrEmpty(path) && !Assemblies.ContainsKey(path)) Assemblies[path] = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)));
    }

    private static void Scan(ModuleDefinition module)
    {
        // Resolve references even if the IL walker does not happen to reach them.
        foreach (var reference in module.AssemblyReferences)
        {
            var assembly = Resolve(() => module.AssemblyResolver.Resolve(reference), reference.FullName);
            if (assembly != null) Remember(assembly.MainModule);
        }
        foreach (var reference in module.GetTypeReferences())
        {
            var type = Resolve(() => reference.Resolve(), reference.FullName);
            if (type != null) Remember(type.Module);
        }
        foreach (var caller in module.GetTypes())
        foreach (var method in caller.Methods.Where(m => m.HasBody))
        foreach (var instruction in method.Body.Instructions)
        {
            var reference = instruction.Operand as MemberReference;
            if (reference == null) continue;
            string site = reference.FullName + " <- " + method.FullName + " IL_" + instruction.Offset.ToString("x4");
            TypeDefinition owner = null;
            bool accessible = true;
            if (reference is MethodReference mr)
            {
                var member = Resolve(() => mr.Resolve(), site);
                if (member == null) continue;
                owner = member.DeclaringType;
                accessible = member.Module == module || member.IsPublic ||
                    ((member.IsFamily || member.IsFamilyOrAssembly) && Derived(caller, owner));
            }
            else if (reference is FieldReference fr)
            {
                var member = Resolve(() => fr.Resolve(), site);
                if (member == null) continue;
                owner = member.DeclaringType;
                accessible = member.Module == module || member.IsPublic ||
                    ((member.IsFamily || member.IsFamilyOrAssembly) && Derived(caller, owner));
            }
            else if (reference is TypeReference tr && !(tr is GenericParameter))
                owner = Resolve(() => tr.Resolve(), site);
            if (owner == null) continue;
            checkedMembers++;
            Remember(owner.Module);
            if (!accessible || !Visible(owner, caller)) Inaccessible.Add(site);
        }
    }

    private static int Main(string[] args)
    {
        if (args.Length < 3) return 2;
        string status = "failed";
        try
        {
            using var resolver = new DefaultAssemblyResolver();
            // Never resolve game APIs from NuGet publicized reference assemblies.
            resolver.RemoveSearchDirectory(".");
            resolver.RemoveSearchDirectory("bin");
            resolver.AddSearchDirectory(Path.GetFullPath(args[1]));
            foreach (string directory in args.Skip(3))
            {
                if (!Directory.Exists(directory)) throw new DirectoryNotFoundException(directory);
                resolver.AddSearchDirectory(Path.GetFullPath(directory));
            }
            using var module = ModuleDefinition.ReadModule(args[0], new ReaderParameters { AssemblyResolver = resolver });
            Scan(module);
            status = checkedMembers > 0 && Unresolved.Count == 0 && Inaccessible.Count == 0 ? "passed" : "failed";
        }
        catch (Exception e) { Unresolved.Add("SCANNER: " + e); }
        File.WriteAllText(args[2], JsonSerializer.Serialize(new {
            schema = 1, status, checkedMembers, candidate = Path.GetFullPath(args[0]),
            candidateSha256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(args[0]))),
            gameManaged = Path.GetFullPath(args[1]), assemblies = Assemblies,
            unresolved = Unresolved.OrderBy(s => s).ToArray(), inaccessible = Inaccessible.OrderBy(s => s).ToArray()
        }, new JsonSerializerOptions { WriteIndented = true }));
        Console.WriteLine("ACCESS_" + status.ToUpperInvariant() + " checked=" + checkedMembers +
            " unresolved=" + Unresolved.Count + " inaccessible=" + Inaccessible.Count);
        return status == "passed" ? 0 : 1;
    }
}
