using Mono.Cecil;
using Mono.Cecil.Cil;
using System.Security.Cryptography;

static class NativeIntegration
{
    internal static void Run(string gameDll,string pluginDll,Action<bool,string> check)
    {
        using var game=AssemblyDefinition.ReadAssembly(gameDll);
        using var plugin=AssemblyDefinition.ReadAssembly(pluginDll);
        var types=game.MainModule.Types;
        foreach(var target in new[]{("VoxelEngine.VoxelMesh",".ctor",3),("VoxelEngine.VoxelItemCollider",".ctor",1),("Manager.ColliderShapeCommon","CreateShape",1)})
        {
            var method=types.Single(t=>t.FullName==target.Item1).Methods.Single(m=>m.Name==target.Item2);
            int loads=method.Body.Instructions.Count(i=>i.Operand is GenericInstanceMethod g && g.DeclaringType.FullName=="UnityEngine.Resources" && g.Name=="Load" && g.GenericArguments.Single().FullName=="UnityEngine.Mesh");
            check(loads==target.Item3,"Exact native mesh-loader transpiler sites: "+target.Item1);
        }
        var shape=types.Single(t=>t.FullName=="Manager.ColliderShapeCommon").NestedTypes.Single(t=>t.Name=="Shape");
        check(shape.Fields.Where(f=>f.HasConstant).All(f=>Convert.ToInt32(f.Constant)<58),"Native enum does not occupy active/legacy collider slots 58-61");
        var alloc=types.Single(t=>t.FullName=="Manager.ColliderShapeResources").Methods.Single(m=>m.Name=="AllocateResources");
        check(alloc.Body.Instructions.Any(i=>i.Operand is sbyte n && n==64),"Native collider table has 64 entries");
        var manager=types.Single(t=>t.FullName=="Manager.GameResourcesManager").Methods.Single(m=>m.Name=="AllocateResources");
        var ready=manager.Body.Instructions.Single(i=>i.Operand is MethodReference m && m.Name=="OnResourceLoaded");
        check(manager.Body.ExceptionHandlers.All(h=>ready.Offset<h.TryStart.Offset || ready.Offset>=h.TryEnd.Offset),"Readiness gate is outside swallowed allocation callback exceptions");
        var guard=plugin.MainModule.Types.Single(t=>t.Name=="ResourceReadyPatch");
        check(guard.CustomAttributes.Any(a=>a.AttributeType.FullName=="HarmonyLib.HarmonyPatch"),"Compiled resource-ready guard has Harmony target");
        check(guard.Methods.Single(m=>m.Name=="Prefix").Body.Instructions.Any(i=>i.Operand is MethodReference m && m.Name=="RequireReadyForResourceCallbacks"),"Compiled readiness hook enforces collision readiness");
        var compatibility=plugin.MainModule.Types.Single(t=>t.FullName=="HullFiller.BuildCompatibility");
        var pinned=compatibility.Fields.Where(f=>f.HasConstant).Select(f=>(string)f.Constant).ToArray();
        check(pinned.Contains(Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(gameDll)))),"Compiled supported-assembly allowlist includes inspected game");
        check(plugin.MainModule.Types.Single(t=>t.FullName=="HullFiller.Plugin").Methods.Single(m=>m.Name=="Awake").Body.Instructions.Any(i=>i.Operand is MethodReference m && m.DeclaringType.FullName=="HullFiller.BuildCompatibility" && m.Name=="IsSupported"),"Compiled startup uses the tested compatibility policy");
    }
}
