using System.Numerics;
using HullFiller;

int checks=0, failed=0;
void Check(bool pass,string message) { checks++; if(!pass) { failed++; if(failed<12) Console.WriteLine("FAIL: "+message); } }
bool Near(Vector3 a,Vector3 b) => Vector3.Distance(a,b)<.002f;
Vector3 Stretch(Vector3 v,Vector3 s) => v + new Vector3(v.X>0?s.X:0,v.Y>.5?s.Y:0,v.Z>0?s.Z:0);
var loads=typeof(OverloadedResources).GetMethods().Where(m=>m.Name=="Load").ToArray();
var typed=loads.Where(m=>m.IsGenericMethodDefinition).Select(m=>m.MakeGenericMethod(typeof(TestAsset))).ToArray();
Check(typed.Count(m=>ResourceMethodPolicy.IsTypedLoad(m,typeof(OverloadedResources),typeof(TestAsset)))==1,"Ambiguous Load(string)/Load<T>(string) overloads select only typed asset load");
Check(!loads.Where(m=>!m.IsGenericMethod).Any(m=>ResourceMethodPolicy.IsTypedLoad(m,typeof(OverloadedResources),typeof(TestAsset))),"Untyped load is not redirected");
var a=HullGeometry.Vertices(false);var b=HullGeometry.Vertices(true);
Check(a.Length==4 && b.Length==4,"Filler has four vertices in each handedness");
var faces=HullGeometry.Faces(false);
Check(faces.Length==12,"Four triangular faces");
if(a.Length==4 && faces.Length==12)
{
    double vol=0;
    int nativeSlopedPlanes=0;
    var center=a.Aggregate(Vector3.Zero,(v,p)=>v+p)/4;
    for(int i=0;i<faces.Length;i+=3)
    {
        var p=a[faces[i]];var q=a[faces[i+1]];var r=a[faces[i+2]];
        var n=Vector3.Cross(q-p,r-p);
        var normal=Vector3.Normalize(n);
        if(Math.Max(Math.Abs(normal.X),Math.Max(Math.Abs(normal.Y),Math.Abs(normal.Z)))<=.99f)nativeSlopedPlanes++;
        Check(Vector3.Dot(n,center-p)<0,"Face normal points outward");
        vol+=Vector3.Dot(p,Vector3.Cross(q,r))/6.0;
    }
    Check(Math.Abs(vol-1.0/6)<1e-6,"Filler volume is one sixth");
    Check(nativeSlopedPlanes==HullGeometry.NativeSlopedPlaneCount,"Native collider omits axis-aligned planes; expected validation matches actual sloped planes");
    Check(a.Select(v=>v.Y+v.Z).All(v=>v>=.49999f),"Filler is outside corner interior");
    Check(a.All(v=>v.Y<=.5f-v.X+.00001f),"Filler lies within wedge");
    Check(a.Where(v=>Math.Abs(v.Y+v.Z-.5)<1e-5).Count()==3,"Three vertices share stock corner mating face");
    for(int i=0;i<4;i++) Check(Near(b[i],new Vector3(-a[i].X,a[i].Y,a[i].Z)),"Mirror reflects exactly");
    for(int x=0;x<=15;x++) for(int y=0;y<=15;y++) for(int z=0;z<=15;z++)
    {
        var s=new Vector3(x,y,z);var v=a.Select(p=>Stretch(p,s)).ToArray();
        double scaled=0;
        for(int i=0;i<faces.Length;i+=3)scaled+=Vector3.Dot(v[faces[i]],Vector3.Cross(v[faces[i+1]],v[faces[i+2]]))/6.0;
        Check(Math.Abs(scaled-(x+1.0)*(y+1)*(z+1)/6)<.001,"Stretched volume agrees with native collision scaling");
    }
}
Check(HullGeometry.CanRegister(1002,1003,1024,false,false),"Free IDs accepted");
foreach(var pair in new[]{(-1,1003),(0,1003),(1024,1003),(1002,1002)})
    Check(!HullGeometry.CanRegister(pair.Item1,pair.Item2,1024,false,false),"Invalid ID pair rejected");
Check(!HullGeometry.CanRegister(1002,1003,1024,true,false),"First ID collision rejected");
Check(!HullGeometry.CanRegister(1002,1003,1024,false,true),"Second ID collision rejected");
Check(!HullGeometry.CanFinishResourceLoad(true,false,false),"Swallowed allocation failure cannot finish loading registered filler blocks");
Check(!HullGeometry.CanFinishResourceLoad(true,true,false),"Replaced collider slots cannot finish loading registered filler blocks");
Check(HullGeometry.CanFinishResourceLoad(true,true,true),"Ready owned colliders allow resource loading");
Check(HullGeometry.CanFinishResourceLoad(false,false,false),"Unregistered disabled mod does not block stock resources");

var axes=new[]{Vector3.UnitX,Vector3.UnitY,Vector3.UnitZ};
var rotations=new List<Quaternion>();
foreach(var rightAxis in axes)foreach(int signR in new[]{-1,1})foreach(var upAxis in axes)foreach(int signU in new[]{-1,1})
{
    var right=rightAxis*signR;var up=upAxis*signU;if(Math.Abs(Vector3.Dot(right,up))>.1)continue;
    var forward=Vector3.Cross(right,up);
    rotations.Add(Quaternion.CreateFromRotationMatrix(new Matrix4x4(right.X,right.Y,right.Z,0,up.X,up.Y,up.Z,0,forward.X,forward.Y,forward.Z,0,0,0,0,1)));
}
Check(rotations.Count==24,"24 native cube rotations");
foreach(var rotation in rotations)foreach(var normal in axes)foreach(var scale in new[]{Vector3.Zero,new Vector3(1,3,7),new Vector3(15,0,2)})foreach(float grid in new[]{.125f,1f,2f})foreach(bool mirrored in new[]{false,true})
{
    var start=new Placement{PivotGrid=new Vector3(64,-32,96),Rotation=rotation,Mirrored=mirrored};
    var origin=new Vector3(16,32,-16);
    var end=PlacementMath.Reflect(start,scale,grid,normal,origin);
    Check(end.Mirrored!=start.Mirrored,"Reflection swaps handedness");
    var twice=PlacementMath.Reflect(end,scale,grid,normal,origin);
    Check(Near(twice.PivotGrid,start.PivotGrid) && Math.Abs(Quaternion.Dot(twice.Rotation,start.Rotation))>.9999 && twice.Mirrored==start.Mirrored,"Double reflection restores placement");
    var oldV=HullGeometry.Vertices(start.Mirrored);var newV=HullGeometry.Vertices(end.Mirrored);
    for(int i=0;i<oldV.Length;i++)
    {
        var world=start.PivotGrid+Vector3.Transform(Stretch(oldV[i],scale)*32*grid,start.Rotation);
        var expected=world-2*Vector3.Dot(world-origin,normal)*normal;
        var actual=end.PivotGrid+Vector3.Transform(Stretch(newV[i],scale)*32*grid,end.Rotation);
        Check(Near(expected,actual),"Every stretched vertex reflects onto its counterpart");
    }
}
if(args.Length==2) NativeIntegration.Run(args[0],args[1],Check);
Console.WriteLine($"{checks} checks, {failed} failed");
return failed==0?0:1;

public sealed class TestAsset { }
public static class OverloadedResources
{
    public static object Load(string path) => new object();
    public static T Load<T>(string path) where T:new() => new T();
}
