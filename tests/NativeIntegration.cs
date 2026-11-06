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
        check(shape.Fields.Where(f=>f.HasConstant).All(f=>!Enumerable.Range(58,4).Contains(Convert.ToInt32(f.Constant))),"Native enum does not occupy active/legacy collider slots 58-61");
        var alloc=types.Single(t=>t.FullName=="Manager.ColliderShapeResources").Methods.Single(m=>m.Name=="AllocateResources");
        check(alloc.Body.Instructions.Any(i=>i.Operand is sbyte n && n==64),"Native collider table has 64 entries");
        var manager=types.Single(t=>t.FullName=="Manager.GameResourcesManager").Methods.Single(m=>m.Name=="AllocateResources");
        var ready=manager.Body.Instructions.Single(i=>i.Operand is MethodReference m && m.Name=="OnResourceLoaded");
        check(manager.Body.ExceptionHandlers.All(h=>ready.Offset<h.TryStart.Offset || ready.Offset>=h.TryEnd.Offset),"Readiness gate is outside swallowed allocation callback exceptions");
        var guard=plugin.MainModule.Types.Single(t=>t.Name=="ResourceReadyPatch");
        check(guard.CustomAttributes.Any(a=>a.AttributeType.FullName=="HarmonyLib.HarmonyPatch"),"Compiled resource-ready guard has Harmony target");
        check(guard.Methods.Single(m=>m.Name=="Prefix").Body.Instructions.Any(i=>i.Operand is MethodReference m && m.Name=="RequireReadyForResourceCallbacks"),"Compiled readiness hook enforces collision readiness");
        var errors=HullFiller.GameContracts.Validate(game,plugin);
        check(errors.Count==0,"Actual game contracts accepted: "+string.Join("; ",errors));
        check(plugin.MainModule.Types.Single(t=>t.FullName=="HullFiller.Plugin").Methods.Single(m=>m.Name=="Awake").Body.Instructions.Any(i=>i.Operand is MethodReference m && m.DeclaringType.FullName=="HullFiller.GameContracts" && m.Name=="RequireCompatible"),"Compiled startup validates dependency contracts");
        void Mutation(string name,Action<AssemblyDefinition> edit,bool accepted)
        {
            using var resolver=new DefaultAssemblyResolver();
            resolver.AddSearchDirectory(Path.GetDirectoryName(gameDll));
            resolver.AddSearchDirectory(AppContext.BaseDirectory);
            using var changed=AssemblyDefinition.ReadAssembly(gameDll,new ReaderParameters{AssemblyResolver=resolver});
            edit(changed);
            using var bytes=new MemoryStream();
            changed.Write(bytes);
            check(!SHA256.HashData(bytes.ToArray()).SequenceEqual(SHA256.HashData(File.ReadAllBytes(gameDll))),"Mutation changes assembly bytes: "+name);
            bytes.Position=0;
            using var reloaded=AssemblyDefinition.ReadAssembly(bytes);
            var failures=HullFiller.GameContracts.Validate(reloaded,plugin);
            check((failures.Count==0)==accepted,name+": "+string.Join("; ",failures));
        }
        Mutation("Changed MVID and version retain compatible features",a=>{a.MainModule.Mvid=Guid.NewGuid();a.Name.Version=new Version(9,9,9,9);},true);
        Mutation("An unrelated new game type retains compatible features",a=>a.MainModule.Types.Add(new TypeDefinition("FutureGame","UnrelatedFeature",TypeAttributes.Public,a.MainModule.TypeSystem.Object)),true);
        Mutation("Unrelated overloads retain compatible features",a=>{
            foreach(var target in new[]{("VoxelEngine.VoxelMesh",".ctor"),("Manager.ColliderShapeCommon","CreateShape"),("VoxelEngine.Brick","LoadAllResourcesRuntime")})
            {
                var method=new MethodDefinition(target.Item2,MethodAttributes.Public,a.MainModule.TypeSystem.Void);
                if(target.Item2!=".ctor")method.IsStatic=true;
                method.Parameters.Add(new ParameterDefinition(a.MainModule.TypeSystem.Int32));
                method.Body.Instructions.Add(Instruction.Create(OpCodes.Ret));
                a.MainModule.GetType(target.Item1).Methods.Add(method);
            }
        },true);
        Mutation("Changed patch signature is rejected",a=>a.MainModule.GetType("VoxelEngine.EntitySymmetry").Methods.Single(m=>m.Name=="ApplySymmetry").ReturnType=a.MainModule.TypeSystem.Int32,false);
        Mutation("Removed required field is rejected",a=>{var t=a.MainModule.GetType("VoxelEngine.VoxelItemCollider");t.Fields.Single(f=>f.Name=="path").Name="changedPath";},false);
        Mutation("Changed static field binding is rejected",a=>a.MainModule.GetType("VoxelEngine.Brick").Fields.Single(f=>f.Name=="BRICK_INDEX").IsStatic=false,false);
        Mutation("Missing BlockFull resize definition is rejected",a=>a.MainModule.GetType("VoxelEngine.ResizeMode").Fields.Single(f=>f.Name=="BlockFull").Name="changedBlockFull",false);
        Mutation("Changed inlined collider stretch enum is rejected",a=>a.MainModule.GetType("Manager.ColliderShapeCommon").NestedTypes.Single(t=>t.Name=="StretchMode").Fields.Single(f=>f.Name=="uniformScale").Constant=8,false);
        Mutation("Changed optional defaults retain explicitly supplied collider arguments",a=>{
            var m=a.MainModule.GetType("Manager.ColliderShapeCommon").Methods.Single(m=>m.Name=="CreateShape");
            m.Parameters[2].Constant=1;m.Parameters[3].Constant=true;
        },true);
        Mutation("Changed mesh-loader pattern is rejected",a=>{var m=a.MainModule.GetType("VoxelEngine.VoxelMesh").Methods.Single(m=>m.Name==".ctor");var i=m.Body.Instructions.First(i=>i.Operand is GenericInstanceMethod g && g.Name=="Load");i.Operand=a.MainModule.ImportReference(typeof(string).GetMethod("ToString",Type.EmptyTypes)!);},false);
        Mutation("Reserved collider enum conflict is rejected",a=>{var t=a.MainModule.GetType("Manager.ColliderShapeCommon").NestedTypes.Single(t=>t.Name=="Shape");t.Fields.First(f=>f.HasConstant).Constant=58;},false);
        Mutation("Changed native geometry layout is rejected",a=>a.MainModule.GetType("Manager.ColliderShapeCommon").NestedTypes.Single(t=>t.Name=="GeoIndexData").Fields.Add(new FieldDefinition("extra",FieldAttributes.Public,a.MainModule.TypeSystem.Int32)),false);
        Mutation("Reduced collider table capacity is rejected",a=>{var m=a.MainModule.GetType("Manager.ColliderShapeResources").Methods.Single(m=>m.Name=="AllocateResources");var i=m.Body.Instructions.Single(i=>i.Operand is sbyte n && n==64);i.Operand=(sbyte)32;},false);
        Mutation("Larger collider table retains compatible features",a=>{var m=a.MainModule.GetType("Manager.ColliderShapeResources").Methods.Single(m=>m.Name=="AllocateResources");var i=m.Body.Instructions.Single(i=>i.Operand is sbyte n && n==64);i.Operand=(sbyte)96;},true);
        Mutation("Swallowed resource-ready failure is rejected",a=>{var m=a.MainModule.GetType("Manager.GameResourcesManager").Methods.Single(m=>m.Name=="AllocateResources");var h=m.Body.ExceptionHandlers.First();h.TryStart=m.Body.Instructions.First();h.TryEnd=m.Body.Instructions.Last();},false);
    }
}
