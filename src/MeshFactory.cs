using UnityEngine;

namespace HullFiller
{
    internal static class MeshFactory
    {
        internal const string LeftPath = "mod_hull_filler/unit";
        internal const string RightPath = "mod_hull_filler/unit_mirrored";
        private static Mesh left, right;

        // Used only at explicitly patched native mesh loaders. Stock paths fall through.
        internal static Mesh Load(string resourcePath)
        {
            bool mirrored;
            if (resourcePath == "Shapes/" + LeftPath) mirrored = false;
            else if (resourcePath == "Shapes/" + RightPath) mirrored = true;
            // Native VoxelMesh makes its own preview copy before mutating preview UVs.
            else if (resourcePath == "Shapes/" + LeftPath + "_preview" || resourcePath == "Shapes/" + RightPath + "_preview") return null;
            else if (resourcePath == "Shapes/" + LeftPath + "_gizmo" || resourcePath == "Shapes/" + RightPath + "_gizmo") return null;
            else return Resources.Load<Mesh>(resourcePath);
            if (mirrored) return right != null ? right : (right = Create(true));
            return left != null ? left : (left = Create(false));
        }

        private static Mesh Create(bool mirrored)
        {
            var points = HullGeometry.Vertices(mirrored);
            var indices = HullGeometry.Faces(mirrored);
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
            var mesh = new Mesh { name = mirrored ? "HullFillerMirrored" : "HullFiller", vertices = vertices,
                triangles = triangles, uv = uv, uv2 = new Vector2[12], colors32 = colors };
            mesh.RecalculateNormals(); mesh.RecalculateTangents(); mesh.RecalculateBounds();
            Object.DontDestroyOnLoad(mesh);
            return mesh;
        }
    }
}
