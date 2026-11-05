using System;
using System.Numerics;
namespace HullFiller
{
    public static class HullGeometry
    {
        public const int NativeSlopedPlaneCount = 2;
        public static bool CanFinishResourceLoad(bool hasRegisteredBlocks, bool collidersReady, bool ownsColliders)
            => !hasRegisteredBlocks || (collidersReady && ownsColliders);
        public static Vector3[] Vertices(bool mirrored)
        {
            float sign = mirrored ? -1 : 1;
            return new[] { new Vector3(-.5f*sign,1,-.5f), new Vector3(-.5f*sign,1,.5f),
                new Vector3(-.5f*sign,0,.5f), new Vector3(.5f*sign,0,.5f) };
        }
        public static Vector3[] Vertices(bool mirrored, bool flatBase)
        {
            if (!flatBase) return Vertices(mirrored);
            var vertices = Vertices(false);
            for (int i=0;i<vertices.Length;i++)
            {
                var p=vertices[i];
                // Rz(+90 degrees), then translate into the standard Y-up hull bounds.
                // The former X=-.5 triangular face becomes the native Y=0 base.
                vertices[i]=new Vector3((mirrored?-1:1)*(.5f-p.Y),p.X+.5f,p.Z);
            }
            return vertices;
        }
        public static int[] Faces(bool mirrored, bool flatBase) => OrientFaces(Vertices(mirrored,flatBase));
        public static bool CanRegisterDefinitions(int[] ids, int length, bool[] occupied)
        {
            if(ids==null || occupied==null || ids.Length!=occupied.Length || ids.Length==0)return false;
            for(int i=0;i<ids.Length;i++)
            {
                if(ids[i]<=0 || ids[i]>=length || occupied[i])return false;
                for(int j=0;j<i;j++)if(ids[i]==ids[j])return false;
            }
            return true;
        }
        public static int[] Faces(bool mirrored)
        {
            return OrientFaces(Vertices(mirrored));
        }
        private static int[] OrientFaces(Vector3[] vertices)
        {
            var faces = new[] { 0,1,2, 0,2,3, 0,3,1, 1,3,2 };
            Vector3 center = (vertices[0]+vertices[1]+vertices[2]+vertices[3])/4;
            for (int i=0;i<faces.Length;i+=3)
            {
                Vector3 p=vertices[faces[i]], q=vertices[faces[i+1]], r=vertices[faces[i+2]];
                if (Vector3.Dot(Vector3.Cross(q-p,r-p),center-p)>0)
                { int swap=faces[i+1];faces[i+1]=faces[i+2];faces[i+2]=swap; }
            }
            return faces;
        }
        public static bool CanRegister(int first, int second, int length, bool firstOccupied, bool secondOccupied)
            => first>0 && second>0 && first<length && second<length && first!=second && !firstOccupied && !secondOccupied;
    }
}
