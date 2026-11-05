using UnityEngine;

namespace HullFiller
{
    internal static class MeshFactory
    {
        internal const string LeftPath = "mod_hull_filler/unit";
        internal const string RightPath = "mod_hull_filler/unit_mirrored";
        internal const string FlatLeftPath = "mod_hull_filler/unit_flat_base";
        internal const string FlatRightPath = "mod_hull_filler/unit_flat_base_mirrored";
        private static readonly string[] Paths = { LeftPath,RightPath,FlatLeftPath,FlatRightPath };
        private static readonly Mesh[] meshes = new Mesh[4];
        internal static string GetPath(bool mirrored,bool flatBase) => Paths[(flatBase?2:0)+(mirrored?1:0)];
        internal static bool IsFillerPath(string path) => System.Array.IndexOf(Paths,path)>=0;

        // Used only at explicitly patched native mesh loaders. Stock paths fall through.
        internal static Mesh Load(string resourcePath)
        {
            for(int i=0;i<Paths.Length;i++)
            {
                string path="Shapes/"+Paths[i];
                if(resourcePath==path)
                    return meshes[i]!=null ? meshes[i] : (meshes[i]=Create((i&1)!=0,i>=2));
                // Native VoxelMesh creates a separate preview before mutating preview UVs.
                if(resourcePath==path+"_preview" || resourcePath==path+"_gizmo")return null;
            }
            return Resources.Load<Mesh>(resourcePath);
        }

        private static Mesh Create(bool mirrored,bool flatBase)
        {
            var points = HullGeometry.Vertices(mirrored,flatBase);
            var indices = HullGeometry.Faces(mirrored,flatBase);
            var vertices = new Vector3[12]; var triangles = new int[12];
            var uv = new Vector2[12]; var colors = new Color32[12];
            for (int face = 0; face < 4; face++)
            {
                int start = face * 3;
                for (int j=0;j<3;j++)
                {
                    var p = points[indices[start+j]];
                    vertices[start+j] = new Vector3(p.X,p.Y,p.Z);
                    triangles[start+j] = start+j;
                    colors[start+j] = new Color32(255,255,255,255);
                }
                Vector3 n = Vector3.Cross(vertices[start+1]-vertices[start],vertices[start+2]-vertices[start]).normalized;
                // Native hull corner's slot 1 recomputes normals after anisotropic stretching.
                bool slope = Mathf.Max(Mathf.Abs(n.x),Mathf.Abs(n.y),Mathf.Abs(n.z)) < .999f;
                float materialSlot = slope ? 1f : 0f;
                uv[start] = new Vector2(materialSlot,0f);
                uv[start+1] = new Vector2(materialSlot+1f,0f);
                uv[start+2] = new Vector2(materialSlot,1f);
            }
            var mesh = new Mesh { name = "HullFiller"+(flatBase?"FlatBase":"Legacy")+(mirrored?"Mirrored":""), vertices = vertices,
                triangles = triangles, uv = uv, uv2 = new Vector2[12], colors32 = colors };
            mesh.RecalculateNormals(); mesh.RecalculateTangents(); mesh.RecalculateBounds();
            Object.DontDestroyOnLoad(mesh);
            return mesh;
        }
    }
}
