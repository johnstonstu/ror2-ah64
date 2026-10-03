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
    }
    public static partial class Mathf
    {
        public static float Sign(float n)=>n>=0?1:-1;
        public static float Sin(float n)=>(float)Math.Sin(n);
        public static float Exp(float n)=>(float)Math.Exp(n);
        public static float Repeat(float n,float length)=>n-(float)Math.Floor(n/length)*length;
        public static float SmoothStep(float a,float b,float t){t=Clamp01(t);return Lerp(a,b,t*t*(3-2*t));}
        public static float InverseLerp(float a,float b,float t)=>a==b?0:Clamp01((t-a)/(b-a));
    }
}
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
