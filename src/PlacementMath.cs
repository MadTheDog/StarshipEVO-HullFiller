using System.Numerics;
namespace HullFiller
{
    public struct Placement
    {
        public Vector3 PivotGrid;
        public Quaternion Rotation;
        public bool Mirrored;
    }
    public static class PlacementMath
    {
        public static Placement Reflect(Placement value, Vector3 addedScale, float gridSize, Vector3 normal, Vector3 originGrid)
        {
            Vector3 ReflectVector(Vector3 v) => v-2*Vector3.Dot(v,normal)*normal;
            Vector3 center = addedScale/2 + new Vector3(0,.5f,0);
            Vector3 oldCenter = value.PivotGrid + Vector3.Transform(center*32*gridSize,value.Rotation);
            Vector3 newCenter = originGrid + ReflectVector(oldCenter-originGrid);
            Vector3 right = -ReflectVector(Vector3.Transform(Vector3.UnitX,value.Rotation));
            Vector3 up = ReflectVector(Vector3.Transform(Vector3.UnitY,value.Rotation));
            Vector3 forward = ReflectVector(Vector3.Transform(Vector3.UnitZ,value.Rotation));
            Quaternion rotation = Quaternion.Normalize(Quaternion.CreateFromRotationMatrix(new Matrix4x4(
                right.X,right.Y,right.Z,0,up.X,up.Y,up.Z,0,forward.X,forward.Y,forward.Z,0,0,0,0,1)));
            return new Placement { Mirrored=!value.Mirrored, Rotation=rotation,
                PivotGrid=newCenter-Vector3.Transform(center*32*gridSize,rotation) };
        }
    }
}
