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
    [BepInPlugin(Id, "Hull Filler", "0.1.2")]
    public sealed class Plugin : BaseUnityPlugin
    {
        public const string Id = "local.sevo.hullfiller";
        internal const int FirstShape = 60, SecondShape = 61;
        internal const int FlatFirstShape = 58, FlatSecondShape = 59;
        private static readonly int[] ShapeSlots = { FlatFirstShape,FlatSecondShape,FirstShape,SecondShape };
        internal static readonly object[] HullArgs = { "Hull/HullA" };
        internal static ManualLogSource Log;
        internal static FillerBrick Left, Right, LegacyLeft, LegacyRight;
        internal static int LeftId, RightId, LegacyLeftId, LegacyRightId;
        private static FillerBrick[] Blocks => new[] { Left,Right,LegacyLeft,LegacyRight };
        private static int[] BlockIds => new[] { LeftId,RightId,LegacyLeftId,LegacyRightId };
        private static bool failed;
        private static bool collidersReady;
        private static readonly ColliderShapeCommon.GeoIndexData[] expected = new ColliderShapeCommon.GeoIndexData[4];
        private Harmony harmony;

        private void Awake()
        {
            Log = Logger;
            LegacyLeftId = Config.Bind("Blocks", "FillerId", 1002, "Legacy v0.1.0/v0.1.1 save ID. Keep unchanged for existing blocks.").Value;
            LegacyRightId = Config.Bind("Blocks", "MirroredFillerId", 1003, "Legacy mirrored save ID. Keep unchanged for existing blocks.").Value;
            LeftId = Config.Bind("Blocks", "FlatBaseFillerId", 1004, "Flat-base filler save ID. Do not change after placing these blocks.").Value;
            RightId = Config.Bind("Blocks", "FlatBaseMirroredFillerId", 1005, "Flat-base mirrored save ID. Must differ from all other filler IDs.").Value;
            try
            {
                string hash;
                using (var file = File.OpenRead(typeof(Brick).Assembly.Location))
                using (var sha = SHA256.Create())
                    hash = BitConverter.ToString(sha.ComputeHash(file)).Replace("-", "");
                if (!BuildCompatibility.IsSupported(hash))
                    throw new NotSupportedException("Unsupported Assembly-CSharp: " + hash + ". Hull Filler 0.1.2 supports inspected Steam builds 26394979 and 26451593 (Unity 6). Update the mod for a different game build.");
                if (!HullGeometry.CanRegisterDefinitions(BlockIds, 1024, new bool[4]))
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
                foreach(var definition in Blocks)
                {
                    if (!ReferenceEquals(Brick.BRICK_INDEX[definition.brickID],definition))
                        throw new InvalidOperationException("Hull Filler save IDs were replaced by another definition.");
                    definition.LoadAfterStock();
                }
                return;
            }
            var occupied = new bool[4]; var ids=BlockIds;
            for(int i=0;i<ids.Length;i++)occupied[i]=Brick.BRICK_INDEX[ids[i]]!=null;
            if (!HullGeometry.CanRegisterDefinitions(ids, Brick.BRICK_INDEX.Length, occupied))
            {
                failed = true;
                Log.LogError("Hull Filler disabled: reserved brick IDs are occupied. No definition overwritten.");
                return;
            }
            Left = new FillerBrick(LeftId, false, true);
            Right = new FillerBrick(RightId, true, true);
            LegacyLeft = new FillerBrick(LegacyLeftId, false, false);
            LegacyRight = new FillerBrick(LegacyRightId, true, false);
            int[] shapes = { LeftId, RightId };
            int[] legacyShapes = { LegacyLeftId, LegacyRightId };
            foreach (FillerBrick brick in Blocks)
            {
                brick.multiShapes = brick.FlatBase ? shapes : legacyShapes;
                brick.LoadAfterStock();
                brick.Desc("Fills the gap between a hull corner and wedge. Stretches on all axes; use the paired shape for opposite handedness.")
                    .Tab(CodexTab.HULL_BLOCKS);
            }
            // Keep legacy icon/eyedropper data but show the face-backed pair in the hull tab.
            CodexTab.HULL_BLOCKS.items.Remove(LegacyLeft);
            Log.LogInfo("Registered Hull Filler and mirrored variant as bricks " + LeftId + "/" + RightId + " with flat triangular placement bases. Legacy bricks " + LegacyLeftId + "/" + LegacyRightId + " retain their saved geometry.");
        }

        internal static bool IsFiller(Brick brick) => brick != null && (ReferenceEquals(brick, Left) || ReferenceEquals(brick, Right) || ReferenceEquals(brick,LegacyLeft) || ReferenceEquals(brick,LegacyRight));
        internal static bool IsMirrored(int id) => id==RightId || id==LegacyRightId;
        internal static int PairedId(int id)
        {
            if(id==LeftId)return RightId;
            if(id==RightId)return LeftId;
            if(id==LegacyLeftId)return LegacyRightId;
            if(id==LegacyRightId)return LegacyLeftId;
            throw new ArgumentException("Unknown filler block ID.");
        }

        internal static void RegisterColliders()
        {
            if (Left == null || failed) return;
            var table = ColliderShapeResources.GeometryDatas;
            foreach(int slot in ShapeSlots)
                if(table.Length<=slot || table[slot].vertexCount!=0)
                    throw new InvalidOperationException("Hull Filler collision slot "+slot+" is occupied. Refusing to load with incorrect geometry.");
            var blocks=Blocks;
            for(int i=0;i<blocks.Length;i++)
            {
                int slot=ShapeSlots[i];
                ColliderShapeCommon.CreateShape((ColliderShapeCommon.Shape)slot,blocks[i].nameMesh);
                if(table[slot].vertexCount!=4 || table[slot].planeCount!=HullGeometry.NativeSlopedPlaneCount)
                    throw new InvalidOperationException("Hull Filler native convex collision validation failed for "+blocks[i].nameMesh);
                expected[i]=table[slot];
            }
            collidersReady = true;
            Log.LogInfo("Hull Filler native convex collision registered (4 vertices, 2 sloped planes plus native bounding planes per variant).");
        }

        internal static void RequireReadyForResourceCallbacks()
        {
            var table=ColliderShapeResources.GeometryDatas;
            bool owns = table.IsCreated && table.Length > SecondShape;
            if(owns)for(int i=0;i<ShapeSlots.Length;i++)owns &= table[ShapeSlots[i]].Equals(expected[i]);
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
        internal readonly bool FlatBase;
        internal FillerBrick(int id, bool mirrored,bool flatBase)
            : base(id, (mirrored ? "Hull Filler (mirrored)" : "Hull Filler")+(flatBase?"":" (legacy)"), MeshFactory.GetPath(mirrored,flatBase),
                ResizeMode.BlockFull, Plugin.HullArgs)
        {
            FlatBase=flatBase;
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
            colliderShape = (ColliderShapeCommon.Shape)(flatBase ? (mirrored?Plugin.FlatSecondShape:Plugin.FlatFirstShape) : (mirrored ? Plugin.SecondShape : Plugin.FirstShape));
        }
        // Wait for stock material IDs on every resource reload.
        protected override void LoadResourcesRuntime() { }
        internal void LoadAfterStock() => base.LoadResourcesRuntime();
    }
}
