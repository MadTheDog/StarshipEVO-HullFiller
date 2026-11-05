using System;
using System.IO;
using System.Security.Cryptography;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using Manager;
using UI.Codex;
using UnityEngine;
using VoxelEngine;

namespace HullFiller
{
    [BepInPlugin(Id, "Hull Filler", "0.1.1")]
    public sealed class Plugin : BaseUnityPlugin
    {
        public const string Id = "local.sevo.hullfiller";
        internal const int FirstShape = 60, SecondShape = 61;
        internal static readonly object[] HullArgs = { "Hull/HullA" };
        internal static ManualLogSource Log;
        internal static FillerBrick Left, Right;
        internal static int LeftId, RightId;
        private static bool failed;
        private static bool collidersReady;
        private static ColliderShapeCommon.GeoIndexData expectedLeft, expectedRight;
        private Harmony harmony;

        private void Awake()
        {
            Log = Logger;
            LeftId = Config.Bind("Blocks", "FillerId", 1002, "Stable save ID, 1-1023. Do not change after placing blocks.").Value;
            RightId = Config.Bind("Blocks", "MirroredFillerId", 1003, "Stable save ID, 1-1023. Must differ from FillerId.").Value;
            try
            {
                string hash;
                using (var file = File.OpenRead(typeof(Brick).Assembly.Location))
                using (var sha = SHA256.Create())
                    hash = BitConverter.ToString(sha.ComputeHash(file)).Replace("-", "");
                if (!BuildCompatibility.IsSupported(hash))
                    throw new NotSupportedException("Unsupported Assembly-CSharp: " + hash + ". Hull Filler 0.1.1 supports inspected Steam builds 26394979 and 26451593 (Unity 6). Update the mod for a different game build.");
                if (!HullGeometry.CanRegister(LeftId, RightId, 1024, false, false))
                    throw new ArgumentException("Invalid or duplicate configured brick IDs.");
                harmony = new Harmony(Id);
                harmony.PatchAll(typeof(Plugin).Assembly);
                Logger.LogInfo("Hull Filler loaded. Waiting for stock hull resources.");
            }
            catch (Exception e)
            {
                harmony?.UnpatchSelf();
                failed = true;
                Logger.LogError("Hull Filler disabled before registration: " + e);
            }
        }

        internal static void Register()
        {
            if (failed) return;
            collidersReady = false;
            if (Left != null)
            {
                if (!ReferenceEquals(Brick.BRICK_INDEX[LeftId], Left) || !ReferenceEquals(Brick.BRICK_INDEX[RightId], Right))
                    throw new InvalidOperationException("Hull Filler save IDs were replaced by another definition.");
                Left.LoadAfterStock(); Right.LoadAfterStock(); return;
            }
            bool occupiedLeft = Brick.BRICK_INDEX[LeftId] != null;
            bool occupiedRight = Brick.BRICK_INDEX[RightId] != null;
            if (!HullGeometry.CanRegister(LeftId, RightId, Brick.BRICK_INDEX.Length, occupiedLeft, occupiedRight))
            {
                failed = true;
                Log.LogError("Hull Filler disabled: reserved brick IDs are occupied. No definition overwritten.");
                return;
            }
            Left = new FillerBrick(LeftId, false);
            Right = new FillerBrick(RightId, true);
            int[] shapes = { LeftId, RightId };
            foreach (FillerBrick brick in new[] { Left, Right })
            {
                brick.multiShapes = shapes;
                brick.LoadAfterStock();
                brick.Desc("Fills the gap between a hull corner and wedge. Stretches on all axes; use the paired shape for opposite handedness.")
                    .Tab(CodexTab.HULL_BLOCKS);
            }
            Log.LogInfo("Registered Hull Filler and mirrored variant as bricks " + LeftId + "/" + RightId + ". Native hull stretching and materials enabled.");
        }

        internal static bool IsFiller(Brick brick) => brick != null && (ReferenceEquals(brick, Left) || ReferenceEquals(brick, Right));

        internal static void RegisterColliders()
        {
            if (Left == null || failed) return;
            var table = ColliderShapeResources.GeometryDatas;
            if (table.Length <= SecondShape || table[FirstShape].vertexCount != 0 || table[SecondShape].vertexCount != 0)
                throw new InvalidOperationException("Hull Filler collision slots 60/61 are occupied. Refusing to load a world with incorrect collision shapes.");
            ColliderShapeCommon.CreateShape((ColliderShapeCommon.Shape)FirstShape, MeshFactory.LeftPath);
            ColliderShapeCommon.CreateShape((ColliderShapeCommon.Shape)SecondShape, MeshFactory.RightPath);
            if (table[FirstShape].vertexCount != 4 || table[SecondShape].vertexCount != 4
                || table[FirstShape].planeCount != HullGeometry.NativeSlopedPlaneCount || table[SecondShape].planeCount != HullGeometry.NativeSlopedPlaneCount)
                throw new InvalidOperationException("Hull Filler native convex collision validation failed.");
            expectedLeft = table[FirstShape]; expectedRight = table[SecondShape];
            collidersReady = true;
            Log.LogInfo("Hull Filler native convex collision registered (4 vertices, 2 sloped planes plus native bounding planes per variant).");
        }

        internal static void RequireReadyForResourceCallbacks()
        {
            bool owns = ColliderShapeResources.GeometryDatas.IsCreated
                && ColliderShapeResources.GeometryDatas.Length > SecondShape
                && ColliderShapeResources.GeometryDatas[FirstShape].Equals(expectedLeft)
                && ColliderShapeResources.GeometryDatas[SecondShape].Equals(expectedRight);
            if (!HullGeometry.CanFinishResourceLoad(Left != null, collidersReady, owns))
            {
                const string error = "Hull Filler collision initialization failed or reserved slots were overwritten. Resource-loaded callbacks are blocked; resolve the mod conflict before loading a world.";
                Log.LogError(error);
                throw new InvalidOperationException(error);
            }
        }
    }

    internal sealed class FillerBrick : Brick
    {
        internal FillerBrick(int id, bool mirrored)
            : base(id, mirrored ? "Hull Filler (mirrored)" : "Hull Filler", mirrored ? MeshFactory.RightPath : MeshFactory.LeftPath,
                ResizeMode.BlockFull, Plugin.HullArgs)
        {
            Brick stock = TabHull.HULL_A;
            systemProfile = stock.systemProfile;
            massForUnitShape = stock.massForUnitShape;
            deactivateAO = stock.deactivateAO;
            isDynamicGridSize = stock.isDynamicGridSize;
            minGridSize = stock.minGridSize;
            gridLevel = stock.gridLevel;
            allowPipeOverlap = stock.allowPipeOverlap;
            useMaterial = stock.useMaterial;
            applyCutoutDamage = stock.applyCutoutDamage;
            canPaint = stock.canPaint;
            defaultPaint = stock.defaultPaint;
            PreviewPaint = stock.PreviewPaint;
            scaleUV = stock.scaleUV;
            colliderShape = (ColliderShapeCommon.Shape)(mirrored ? Plugin.SecondShape : Plugin.FirstShape);
        }
        // Wait for stock material IDs on every resource reload.
        protected override void LoadResourcesRuntime() { }
        internal void LoadAfterStock() => base.LoadResourcesRuntime();
    }
}
