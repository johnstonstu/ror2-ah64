using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json;
using Mono.Cecil;

// Models only the existing Unity Mono RequestMinimum declaration for an explicit
// reviewed candidate/runtime/site set. It never writes permissions or runtime files.
internal static class AccessPolicy
{
    internal const string Name = "unity-mono-requestminimum-skipverification-v1";
    internal sealed class Identity { public string path { get; set; } public string sha256 { get; set; } }
    internal sealed class Runtime { public Identity unityPlayer { get; set; } public Identity mono { get; set; } }
    internal sealed class Manifest {
        public int schema { get; set; } public string policy { get; set; } public string candidateSha256 { get; set; }
        public Identity reviewEvidence { get; set; } public Runtime runtime { get; set; } public string[] reviewedSites { get; set; }
    }
    internal sealed class Declaration {
        public bool requestMinimumSkipVerification { get; set; } public bool moduleUnverifiable { get; set; }
    }
    internal sealed class Result {
        public string mode { get; set; } = "strict"; public Identity manifest { get; set; }
        public Manifest reviewedPolicy { get; set; } public Declaration declaration { get; set; }
        public string[] supported { get; set; } = Array.Empty<string>(); public List<string> errors { get; set; } = new List<string>();
    }
    internal static string Hash(string path) => Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(path)));

    private static Declaration Inspect(ModuleDefinition module)
    {
        bool skip = module.Assembly.SecurityDeclarations.Any(d => d.Action == SecurityAction.RequestMinimum &&
            d.SecurityAttributes.Any(a => a.AttributeType.FullName == "System.Security.Permissions.SecurityPermissionAttribute" &&
                a.Properties.Any(p => p.Name == "SkipVerification" && p.Argument.Type.FullName == "System.Boolean" && p.Argument.Value is bool value && value)));
        return new Declaration { requestMinimumSkipVerification = skip,
            moduleUnverifiable = module.CustomAttributes.Any(a => a.AttributeType.FullName == "System.Security.UnverifiableCodeAttribute") };
    }

    private static void VerifyFile(Identity identity, string expectedPath = null)
    {
        if (identity == null || string.IsNullOrEmpty(identity.path) || !Path.IsPathRooted(identity.path) || identity.sha256?.Length != 64)
            throw new InvalidDataException("Policy requires an absolute path and SHA256 for each identity.");
        if (expectedPath != null && !string.Equals(Path.GetFullPath(identity.path), Path.GetFullPath(expectedPath), StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("Policy runtime path differs from the scanned game: " + identity.path);
        if (!string.Equals(Hash(identity.path), identity.sha256, StringComparison.OrdinalIgnoreCase))
            throw new InvalidDataException("Policy file hash mismatch: " + identity.path);
    }

    internal static Result Evaluate(ModuleDefinition module, string policyPath, string gameManaged, string candidateHash,
        HashSet<string> inaccessible, HashSet<string> eligibleMemberSites)
    {
        var result = new Result { declaration = Inspect(module) };
        if (policyPath == null) return result;
        result.mode = Name;
        try {
            result.manifest = new Identity { path = Path.GetFullPath(policyPath), sha256 = Hash(policyPath) };
            var policy = JsonSerializer.Deserialize<Manifest>(File.ReadAllText(policyPath)); result.reviewedPolicy = policy;
            if (policy == null || policy.schema != 1 || policy.policy != Name) throw new InvalidDataException("Unknown runtime policy schema/name.");
            if (!string.Equals(candidateHash, policy.candidateSha256, StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("Policy candidate hash mismatch.");
            if (!result.declaration.requestMinimumSkipVerification || !result.declaration.moduleUnverifiable)
                throw new InvalidDataException("Candidate lacks existing RequestMinimum SkipVerification=true and/or module UnverifiableCode declaration.");
            VerifyFile(policy.reviewEvidence);
            string game = Directory.GetParent(Directory.GetParent(Path.GetFullPath(gameManaged)).FullName).FullName;
            VerifyFile(policy.runtime?.unityPlayer, Path.Combine(game, "UnityPlayer.dll"));
            VerifyFile(policy.runtime?.mono, Path.Combine(game, "MonoBleedingEdge", "EmbedRuntime", "mono-2.0-bdwgc.dll"));
            if (policy.reviewedSites == null || policy.reviewedSites.Length == 0 || policy.reviewedSites.Any(string.IsNullOrWhiteSpace))
                throw new InvalidDataException("Policy requires exact reviewed sites.");
            var reviewed = new HashSet<string>(policy.reviewedSites, StringComparer.Ordinal);
            if (reviewed.Count != policy.reviewedSites.Length || !reviewed.SetEquals(inaccessible))
                throw new InvalidDataException("Non-public sites differ from the exact reviewed set; re-review required.");
            if (!reviewed.IsSubsetOf(eligibleMemberSites)) throw new InvalidDataException("Policy only models method/field visibility on accessible declaring types.");
            result.supported = reviewed.OrderBy(s => s, StringComparer.Ordinal).ToArray();
        } catch (Exception e) { result.errors.Add(e.GetType().Name + ": " + e.Message); }
        return result;
    }
}
