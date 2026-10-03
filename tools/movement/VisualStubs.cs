using System;
namespace UnityEngine
{
    [AttributeUsage(AttributeTargets.Class)]public class DefaultExecutionOrder:Attribute{public DefaultExecutionOrder(int n){}}
    public static class Shader {public static int PropertyToID(string s)=>0;}
    public class Material:Object {public bool HasProperty(int id)=>true;public Color GetColor(int id)=>new Color();}
    public class Renderer:Component {public Material sharedMaterial;public void SetPropertyBlock(MaterialPropertyBlock b){} }
    public class MaterialPropertyBlock {public void SetColor(int id,Color c){} }
    public struct Color {public float r,g,b,a;public Color(float r,float g,float b,float a){this.r=r;this.g=g;this.b=b;this.a=a;}}
    public struct Vector2 {public float x,y;public Vector2(float x,float y){this.x=x;this.y=y;}public float magnitude=>(float)Math.Sqrt(x*x+y*y);}
    public static class Time {public static float deltaTime=.001f,time;}
    public enum QueryTriggerInteraction {Ignore}
    public struct RaycastHit {public float distance;public Vector3 point,normal;}
    public static class Physics
    {public static bool Raycast(Vector3 p,Vector3 d,out RaycastHit hit,float length,int mask,QueryTriggerInteraction q){hit=new RaycastHit();return false;}}
    public partial struct Vector3
    {
        public static Vector3 down=>new Vector3(0,-1,0);
        public static Vector3 Lerp(Vector3 a,Vector3 b,float t)=>a+(b-a)*Mathf.Clamp01(t);
        public static float SignedAngle(Vector3 a,Vector3 b,Vector3 axis)
        {return (float)(Math.Atan2(Dot(axis,Cross(a,b)),Dot(a,b))*180/Math.PI);}
    }
    public partial struct Quaternion
    {
        public static Quaternion FromToRotation(Vector3 a,Vector3 b)=>identity; private System.Numerics.Quaternion N=>new System.Numerics.Quaternion(x,y,z,w);
        private static Quaternion Q(System.Numerics.Quaternion q)=>new Quaternion(q.X,q.Y,q.Z,q.W);
        public static Quaternion operator *(Quaternion a,Quaternion b)=>Q(a.N*b.N);
        public static Quaternion Euler(float pitch,float yaw,float roll)
        {float r=(float)Math.PI/180;return Q(System.Numerics.Quaternion.CreateFromAxisAngle(System.Numerics.Vector3.UnitY,yaw*r)
            *System.Numerics.Quaternion.CreateFromAxisAngle(System.Numerics.Vector3.UnitX,pitch*r)
            *System.Numerics.Quaternion.CreateFromAxisAngle(System.Numerics.Vector3.UnitZ,roll*r));}
        public static Quaternion Slerp(Quaternion a,Quaternion b,float t)=>Q(System.Numerics.Quaternion.Slerp(a.N,b.N,Mathf.Clamp01(t)));
        public static Quaternion Inverse(Quaternion q)=>Q(System.Numerics.Quaternion.Inverse(q.N));
        public static Quaternion AngleAxis(float a,Vector3 axis)=>Q(System.Numerics.Quaternion.CreateFromAxisAngle(new System.Numerics.Vector3(axis.x,axis.y,axis.z),a*(float)Math.PI/180));
        public static Vector3 operator *(Quaternion q,Vector3 v)
        {var n=System.Numerics.Vector3.Transform(new System.Numerics.Vector3(v.x,v.y,v.z),q.N);return new Vector3(n.X,n.Y,n.Z);}
        public static float Angle(Quaternion a,Quaternion b)
        {double dot=Math.Abs((double)a.x*b.x+(double)a.y*b.y+(double)a.z*b.z+(double)a.w*b.w);double norm=Math.Sqrt(((double)a.x*a.x+(double)a.y*a.y+(double)a.z*a.z+(double)a.w*a.w)*((double)b.x*b.x+(double)b.y*b.y+(double)b.z*b.z+(double)b.w*b.w));return (float)(Math.Acos(Math.Min(1,dot/norm))*360/Math.PI);}
    }
    public static partial class Mathf
    {
        public const float Rad2Deg=180f/(float)Math.PI;
        public static float Atan2(float y,float x)=>(float)Math.Atan2(y,x);
        public static float Sign(float n)=>n>=0?1:-1;
        public static float Sin(float n)=>(float)Math.Sin(n);
        public static float Exp(float n)=>(float)Math.Exp(n);
        public static float Repeat(float n,float length)=>n-(float)Math.Floor(n/length)*length;
        public static float SmoothStep(float a,float b,float t){t=Clamp01(t);return Lerp(a,b,t*t*(3-2*t));}
        public static float InverseLerp(float a,float b,float t)=>a==b?0:Clamp01((t-a)/(b-a));
    }
}
namespace UnityEngine.SceneManagement
{
    public struct Scene { }
    public static class SceneManager
    {
        public static event Action<Scene,Scene> activeSceneChanged;
        public static void Change()=>activeSceneChanged?.Invoke(new Scene(),new Scene());
    }
}
#if VISUALS
namespace AH64.Survivors.SkillStates
{
    // Type/priority doubles only. Actual utility state/motor execution is checked in their own suites.
    public sealed class BrakingTurn:EntityStates.BaseSkillState
    {public override EntityStates.InterruptPriority GetMinimumInterruptPriority()=>EntityStates.InterruptPriority.Pain;}
    public sealed class ServoDash:EntityStates.BaseSkillState
    {public override EntityStates.InterruptPriority GetMinimumInterruptPriority()=>EntityStates.InterruptPriority.Pain;}
    public sealed class SmokeBackflip:EntityStates.BaseSkillState
    {public override EntityStates.InterruptPriority GetMinimumInterruptPriority()=>EntityStates.InterruptPriority.Pain;}
}
#endif
public class ChildLocator:UnityEngine.Component {public UnityEngine.Transform FindChild(string s)=>null;}
namespace RoR2
{
    public class ModelLocator:UnityEngine.Component
    {public UnityEngine.Transform modelTransform,modelBaseTransform;public bool autoUpdateModelTransform=true;}
    public enum VisibilityLevel {Invisible,Visible}
    public class CharacterModel:UnityEngine.Component {public bool isActiveAndEnabled=true;public int invisibilityCount;public VisibilityLevel visibility=VisibilityLevel.Visible;}
    public class HealthComponent:UnityEngine.Component {public bool alive=true;public float combinedHealth,fullCombinedHealth=100,combinedHealthFraction=1;}
    public struct LayerIndex {public static LayerIndex world;public int mask;}
}
