using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using Manager;
using UnityEngine;
using VoxelEngine;
using NVector3 = System.Numerics.Vector3;
using NQuaternion = System.Numerics.Quaternion;

namespace HullFiller
{
    [HarmonyPatch]
    internal static class MeshLoaderPatch
    {
        private static IEnumerable<MethodBase> TargetMethods()
        {
            yield return AccessTools.Constructor(typeof(VoxelMesh), new[] { typeof(string),typeof(Brick),typeof(float),typeof(ResizeMode),typeof(bool),typeof(bool) });
            yield return AccessTools.Constructor(typeof(VoxelItemCollider), new[] { typeof(VoxelMesh),typeof(ColliderShapeCommon.Shape),typeof(Bounds),typeof(SubShape[]) });
            yield return AccessTools.Method(typeof(ColliderShapeCommon),nameof(ColliderShapeCommon.CreateShape));
        }
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase __originalMethod)
        {
            MethodInfo replacement = AccessTools.Method(typeof(MeshFactory), nameof(MeshFactory.Load));
            int changed=0;
            foreach (var instruction in instructions)
            {
                if (instruction.operand is MethodInfo method
                    && ResourceMethodPolicy.IsTypedLoad(method,typeof(Resources),typeof(Mesh)))
                { instruction.operand=replacement;changed++; }
                yield return instruction;
            }
            int expected = __originalMethod.DeclaringType == typeof(VoxelMesh) ? 3 : 1;
            if (changed!=expected) throw new InvalidOperationException("Unexpected mesh loader layout in " + __originalMethod + ": " + changed);
        }
    }

    [HarmonyPatch(typeof(Brick),nameof(Brick.LoadAllResourcesRuntime))]
    internal static class RegistrationPatch { private static void Postfix() => Plugin.Register(); }

    [HarmonyPatch(typeof(ColliderShapeResources),nameof(ColliderShapeResources.AllocateResources))]
    internal static class ColliderRegistrationPatch { private static void Postfix() => Plugin.RegisterColliders(); }

    // The allocator swallows callback exceptions; this uncaught boundary must also gate success.
    [HarmonyPatch(typeof(GameResourcesManager),nameof(GameResourcesManager.OnResourceLoaded))]
    internal static class ResourceReadyPatch { private static void Prefix() => Plugin.RequireReadyForResourceCallbacks(); }

    [HarmonyPatch(typeof(VoxelMaterial),nameof(VoxelMaterial.CreateFaceIDArrayRuntime))]
    internal static class HullMaterialPatch
    {
        private static bool Prefix(object[] __0,ref VoxelMaterial[] __result)
        {
            if (!ReferenceEquals(__0,Plugin.HullArgs)) return true;
            __result = TabHull.HULL_A_CORNER.VoxelMaterials;
            if (__result==null || __result.Length<2) throw new InvalidOperationException("Stock hull corner materials not loaded.");
            return false;
        }
    }

    [HarmonyPatch(typeof(VoxelItemCollider),nameof(VoxelItemCollider.GetShapeFactor))]
    internal static class VolumeFactorPatch
    {
        private static void Postfix(string ___path,ref float __result)
        {
            if (___path==MeshFactory.LeftPath || ___path==MeshFactory.RightPath) __result/=6f;
        }
    }

    [HarmonyPatch(typeof(EntitySymmetry),nameof(EntitySymmetry.ApplySymmetry))]
    internal static class SymmetryPatch
    {
        private static bool Prefix(ref BrickInstanceData brickData,SymmetryPlane plane,ref bool __result)
        {
            if (!Plugin.IsFiller(brickData.Brick)) return true;
            __result=true;
            if (!plane.IsValid) return false;
            var q=brickData.Quaternion;
            var start=new Placement { PivotGrid=ToNumerics(brickData.gridPosition.ToVector3()),
                Rotation=new NQuaternion(q.x,q.y,q.z,q.w), Mirrored=brickData.brickId==Plugin.RightId };
            var result=PlacementMath.Reflect(start,ToNumerics(brickData.ScaleFloat),brickData.GridSize,
                ToNumerics(plane.planeOrientation.normal),32*ToNumerics(plane.origin));
            // Resolve to a native discrete rotation instead of serializing arbitrary quaternions.
            var r=result.Rotation; var target=new Quaternion(r.X,r.Y,r.Z,r.W);
            float best=-1;byte rotation=0;
            for (int i=0;i<VoxelRotation.Quaternions.Length;i++)
            {
                float dot=Mathf.Abs(Quaternion.Dot(target,VoxelRotation.Quaternions[i]));
                if (dot>best) {best=dot;rotation=(byte)i;}
            }
            if (best<.9999f) throw new InvalidOperationException("Hull Filler symmetry is not a native block rotation.");
            brickData.rotation=rotation;
            brickData.brickId=(ushort)(result.Mirrored?Plugin.RightId:Plugin.LeftId);
            brickData.gridPosition=new Vector3i(Mathf.RoundToInt(result.PivotGrid.X),Mathf.RoundToInt(result.PivotGrid.Y),Mathf.RoundToInt(result.PivotGrid.Z));
            return false;
        }
        private static NVector3 ToNumerics(Vector3 v) => new NVector3(v.x,v.y,v.z);
    }
}
