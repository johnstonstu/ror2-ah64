using System;
using System.Reflection;
using AH64.Survivors.Components;
using RoR2;
using UnityEngine;
internal static class VisualChecks
{
    private static int assertions;
    private static void Check(bool b,string id){assertions++;if(!b)throw new Exception(id);}
    private static void Call(object v,string method)=>v.GetType().GetMethod(method,BindingFlags.Instance|BindingFlags.NonPublic).Invoke(v,null);
    private static float Angle(Quaternion a,Quaternion b)
    {double dot=Math.Abs((double)a.x*b.x+(double)a.y*b.y+(double)a.z*b.z+(double)a.w*b.w);double norm=Math.Sqrt(((double)a.x*a.x+(double)a.y*a.y+(double)a.z*a.z+(double)a.w*a.w)*((double)b.x*b.x+(double)b.y*b.y+(double)b.z*b.z+(double)b.w*b.w));return (float)(Math.Acos(Math.Min(1,dot/norm))*360/Math.PI);}
    private static GameObject Body()
    {
        var g=new GameObject();g.AddComponent<CharacterMotor>();g.AddComponent<CharacterDirection>();
        g.AddComponent<CharacterBody>();g.AddComponent<InputBankTest>();g.AddComponent<AH64HoverController>();
        g.AddComponent<CameraTargetParams>().fovOverride=83;
        var locator=g.AddComponent<ModelLocator>();locator.modelTransform=new GameObject().transform;
        locator.modelTransform.gameObject.AddComponent<CharacterModel>();locator.modelBaseTransform=new GameObject().transform;
        g.AddComponent<AH64FlightVisuals>();return g;
    }
    public static void Main()
    {
        foreach(bool flip in new[]{false,true})
        {
            var g=Body();var v=g.GetComponent<AH64FlightVisuals>();var locator=g.GetComponent<ModelLocator>();var owner=new object();
            Call(v,"Start");var entry=Quaternion.Euler(16,0,-22);
            if(flip)v.PlayBackflip(owner,1,entry);else v.PlayBarrelRoll(owner,-1,1,entry);
            Call(v,"LateUpdate");Check(Angle(v.CaptureAttitude(),entry)<.06f,"entry attitude continuity");
            Check(g.GetComponent<CharacterDirection>().forward==Vector3.forward,"cosmetic rotation leaves facing");
            Check(g.GetComponent<CharacterMotor>().velocity==Vector3.zero,"cosmetic rotation leaves motor");

            v.SetManeuverProgress(owner,.5f);Call(v,"LateUpdate");
            Check(Angle(v.CaptureAttitude(),Quaternion.identity)>140,"visible half revolution");
            var middle=v.CaptureAttitude();v.EndManeuver(new object());Call(v,"LateUpdate");Check(Angle(v.CaptureAttitude(),middle)<.06f,"stale owner cannot cancel");
            v.EndManeuver(owner);Call(v,"LateUpdate");Check(Angle(v.CaptureAttitude(),middle)<3,"interrupted recovery continuous");
            Check(g.GetComponent<CameraTargetParams>().fovOverride==83,"prior camera FOV restored");
            g.GetComponent<CameraTargetParams>().fovOverride=77;v.EndManeuver(null);
            Check(g.GetComponent<CameraTargetParams>().fovOverride==77,"repeated cleanup leaves replacing camera alone");
            g.GetComponent<CameraTargetParams>().fovOverride=83;
            if(flip)v.PlayBackflip(owner,1,entry);else v.PlayBarrelRoll(owner,1,1,entry);
            v.SetManeuverProgress(owner,1);Call(v,"LateUpdate");
            Check(Angle(v.CaptureAttitude(),Quaternion.identity)<.06f,"completed revolution recovers flight attitude");
            Call(v,"OnDisable");Check(locator.autoUpdateModelTransform,"disable returns transform ownership");
            Check(g.GetComponent<CameraTargetParams>().fovOverride==83,"disable restores FOV");
            Call(v,"OnEnable");Check(!locator.autoUpdateModelTransform,"enable reclaims transform ownership");
            if(flip)v.PlayBackflip(owner,1,entry);else v.PlayBarrelRoll(owner,1,1,entry);
            v.PlayCrash(1);Check(g.GetComponent<CameraTargetParams>().fovOverride==83,"death clears FOV");Call(v,"OnDestroy");
        }
        int starts=RoR2.Util.sounds.FindAll(s=>s=="Play_loader_m2_travel_loop").Count;
        int stops=RoR2.Util.sounds.FindAll(s=>s=="Stop_loader_m2_travel_loop").Count;
        Check(starts==stops,"owned travel audio balanced on exit/disable/crash/destroy");
        Console.WriteLine("Movement visual offline checks passed: "+assertions+" assertions (API doubles; runtime unverified).");
    }
}
