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
        public static int[] Faces(bool mirrored)
        {
            var vertices = Vertices(mirrored);
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
