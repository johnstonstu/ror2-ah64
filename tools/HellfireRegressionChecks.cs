using System;
using AH64.Survivors.Components;
using RoR2.Projectile;
using UnityEngine;
using UnityEngine.Networking;

internal static partial class HellfireChecks
{
    private static Vector3 AppliedVelocity(Vector3 previous,Vector3 next,float dt)
    {
        Vector3 axis=Vector3.Cross(previous,next);
        return axis.magnitude>0?axis/axis.magnitude*(Angle(previous,next)/dt):Vector3.zero;
    }

    private static void SteeringRegressions()
    {
        foreach(float dt in new[] {0.005f,0.02f,0.05f})
        {
            Vector3 heading=Vector3.forward,velocity=Vector3.zero;
            float budget=240;
            // Longer than the former ramp time: this used to precharge the rate to 120 degrees/s.
            for(int tick=0;tick<100;tick++)
                heading=AH64HellfireAim.Turn(heading,Vector3.forward,dt,ref velocity,ref budget);
            Point(heading,Vector3.forward,"aligned flight stays straight");
            Near(velocity.magnitude,0,0.0001f,"aligned flight has no stored turning");
            Vector3 previous=heading;
            heading=AH64HellfireAim.Turn(heading,new Vector3(1,0,0),dt,ref velocity,ref budget);
            Vector3 actual=AppliedVelocity(previous,heading,dt);
            Check(actual.magnitude<=720*dt+0.002f,"aligned then sudden turn respects actual acceleration");
            Near((actual-velocity).magnitude,0,0.002f,"stored rate equals applied turn");

            float total=Angle(previous,heading);
            Vector3 previousActual=actual;
            // Alternate left/right/up/down while already turning. Compare actual angular vectors,
            // not only scalar magnitudes: a full-rate reversal must ramp through deceleration.
            Vector3[] targets={new Vector3(1,0,0),new Vector3(-1,0,0),Vector3.up,new Vector3(0,-1,0)};
            for(int tick=0;tick<600;tick++)
            {
                previous=heading;float before=budget;
                heading=AH64HellfireAim.Turn(heading,targets[(tick/12)%targets.Length],dt,ref velocity,ref budget);
                actual=AppliedVelocity(previous,heading,dt);
                float turned=Angle(previous,heading);total+=turned;
                Check(turned<=120*dt+0.002f,"variable timestep turn cap");
                Check(budget>=0 && turned<=before+0.002f,"variable timestep remaining budget cap");
                // The hard terminal budget clamp may stop turning immediately; it cannot add a turn.
                if(before>120*dt+0.01f)
                    Check((actual-previousActual).magnitude<=720*dt+0.02f,"direction changes bound actual angular acceleration");
                Near((actual-velocity).magnitude,0,0.004f,"actual angular velocity retained each tick");
                previousActual=actual;
            }
            Check(total<=240.05f,"direction changes spend one whole-life budget across timesteps");
        }
    }

    // Mirrors stock ordering: native impact marks the warhead terminal, but FixedUpdate detonates.
    private sealed class DeferredWarhead : Component,IProjectileImpactBehavior
    {
        public bool alive=true;
        public int impacts,detonations;
        public Vector3 detonationPoint;
        public void OnProjectileImpact(ProjectileImpactInfo impact)
        {
            if(!alive)return;
            alive=false;impacts++;
        }
        private void FixedUpdate()
        {
            if(!alive && detonations==0){detonations++;detonationPoint=transform.position;}
        }
    }

    private static void NativeDeferredImpact()
    {
        NetworkServer.active=true;
        var body=Body(true,951);
        var missile=Missile(body);var go=missile.gameObject;
        var warhead=go.AddComponent<DeferredWarhead>();
        var guard=go.AddComponent<AH64HellfireCollision>();
        var originalCollider=new GameObject().AddComponent<Collider>();
        var nextCollider=new GameObject().AddComponent<Collider>();
        Vector3 nativePoint=new Vector3(0,0,10);
        go.transform.position=nativePoint;
        go.GetComponent<Rigidbody>().velocity=Vector3.forward*140;
        go.GetComponent<ProjectileController>().NativeImpact(new ProjectileImpactInfo
            { collider=originalCollider,estimatedPointOfImpact=nativePoint });
        Check(!warhead.alive && warhead.detonations==0,"native impact is terminal before deferred detonation");
        // The original hit collider moves away before the next tick. A tempting later obstruction
        // would make the old guard reposition the already terminal projectile two metres farther.
        originalCollider.transform.position=new Vector3(20,0,10);
        Physics.Overlaps=Array.Empty<Collider>();
        Physics.Sweeps=new[] {new RaycastHit {collider=nextCollider,distance=2,point=new Vector3(0,0,12)}};
        int before=Physics.CollisionQueries;
        Lifecycle.Call(guard,"FixedUpdate"); // -190 runs ahead of stock warhead FixedUpdate.
        Check(Physics.CollisionQueries==before,"terminal native impact skips all guard queries");
        Point(go.transform.position,nativePoint,"moving-away collider cannot relocate pending native blast");
        Near(go.GetComponent<ProjectileSimple>().desiredForwardSpeed,140,0,"guard preserves native flight writer after native impact");
        Lifecycle.Call(warhead,"FixedUpdate");
        Point(warhead.detonationPoint,nativePoint,"deferred blast remains at native detonation point");
        Lifecycle.Call(guard,"FixedUpdate");Lifecycle.Call(warhead,"FixedUpdate");
        go.GetComponent<ProjectileController>().NativeImpact(new ProjectileImpactInfo {collider=nextCollider});
        Check(warhead.impacts==1 && warhead.detonations==1,"native deferred impact produces one payload");
        Physics.Sweeps=Array.Empty<RaycastHit>();
    }
}
