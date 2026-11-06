using System;
using System.Collections.Generic;
using System.Linq;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace HullFiller
{
    // Metadata inspection does not initialize game types or execute game code.
    public static class GameContracts
    {
        public static void RequireCompatible(string gamePath, string pluginPath)
        {
            using (var game = AssemblyDefinition.ReadAssembly(gamePath))
            using (var plugin = AssemblyDefinition.ReadAssembly(pluginPath))
            {
                var errors = Validate(game, plugin);
                if (errors.Count != 0)
                    throw new NotSupportedException("Hull Filler dependency contracts failed: " + string.Join("; ", errors));
            }
        }

        public static List<string> Validate(AssemblyDefinition game, AssemblyDefinition plugin)
        {
            var errors = new List<string>();
            var types = game.MainModule.GetTypes().ToDictionary(t => t.FullName);
            ValidateBindings(types, plugin, errors);
            ValidateMeshLoader(types, "VoxelEngine.VoxelMesh", ".ctor", 3, errors);
            ValidateMeshLoader(types, "VoxelEngine.VoxelItemCollider", ".ctor", 1, errors);
            ValidateMeshLoader(types, "Manager.ColliderShapeCommon", "CreateShape", 1, errors);

            if (types.TryGetValue("Manager.ColliderShapeCommon/Shape", out var shape))
            {
                foreach (var field in shape.Fields.Where(f => f.HasConstant))
                    if (Convert.ToInt32(field.Constant) >= 58 && Convert.ToInt32(field.Constant) <= 61)
                        errors.Add("Reserved collider slot occupied by " + field.Name);
            }
            else errors.Add("Missing collider Shape enum");

            // The explicitly supplied uniformScale enum is inlined by the C# compiler.
            if (!types.TryGetValue("Manager.ColliderShapeCommon/StretchMode", out var stretchMode)
                || !stretchMode.Fields.Any(f => f.Name == "uniformScale" && f.HasConstant && Convert.ToInt32(f.Constant) == 0))
                errors.Add("Inlined CreateShape uniformScale enum value changed");

            var allocate = Method(types, "Manager.ColliderShapeResources", "AllocateResources", errors);
            if (allocate != null && !allocate.Body.Instructions.Any(i =>
                i.OpCode.Code == Code.Stsfld && i.Operand is FieldReference f && f.Name == "GeometryDatas"
                && i.Previous?.Operand is MethodReference ctor && ctor.Name == ".ctor"
                && ctor.DeclaringType.FullName == "Unity.Collections.NativeArray`1<Manager.ColliderShapeCommon/GeoIndexData>"
                && ctor.Parameters.Count == 3
                && Constant(i.Previous.Previous?.Previous?.Previous) >= 62))
                errors.Add("Collider GeometryDatas allocation capacity cannot be validated");

            var manager = Method(types, "Manager.GameResourcesManager", "AllocateResources", errors);
            if (manager != null)
            {
                var ready = manager.Body.Instructions.Where(i => i.Operand is MethodReference m
                    && m.DeclaringType.FullName == "Manager.GameResourcesManager" && m.Name == "OnResourceLoaded").ToArray();
                if (ready.Length != 1 || manager.Body.ExceptionHandlers.Any(h =>
                    ready[0].Offset >= h.TryStart.Offset && (h.TryEnd == null || ready[0].Offset < h.TryEnd.Offset)))
                    errors.Add("Resource-ready boundary is missing, ambiguous or inside a swallowed exception region");
            }

            // These native fields describe the collision geometry consumed by the plugin.
            if (!types.TryGetValue("Manager.ColliderShapeCommon/GeoIndexData", out var geo)
                || string.Join(",", geo.Fields.Where(f => !f.IsStatic).Select(f => f.Name + "=" + f.FieldType.FullName)) !=
                "shape=Manager.ColliderShapeCommon/Shape,vertexStart=System.Int32,vertexCount=System.Int32,edgeStart=System.Int32,edgeCount=System.Int32,edgeForPointStart=System.Int32,edgeForPointCount=System.Int32,planeStart=System.Int32,planeCount=System.Int32,stretchMode=Manager.ColliderShapeCommon/StretchMode,baseBounds=Common.Aabb,isConcave=System.Boolean,baseScale=Unity.Mathematics.float3"
                || !geo.IsSequentialLayout || geo.HasLayoutInfo)
                errors.Add("Native GeoIndexData field layout changed");
            return errors;
        }

        private static void ValidateBindings(Dictionary<string, TypeDefinition> types, AssemblyDefinition plugin, List<string> errors)
        {
            foreach (var member in plugin.MainModule.GetMemberReferences())
            {
                var declaring = member.DeclaringType.GetElementType();
                if (declaring.Scope.Name != "Assembly-CSharp") continue;
                if (!types.TryGetValue(declaring.FullName, out var type))
                {
                    errors.Add("Missing game type " + declaring.FullName);
                    continue;
                }
                if (member is MethodReference method && !type.Methods.Any(m => m.FullName == method.FullName
                    && m.HasThis == method.HasThis && (m.IsPublic || m.IsFamily || m.IsFamilyOrAssembly)))
                    errors.Add("Missing or inaccessible game method " + method.FullName);
                if (member is FieldReference field && !type.Fields.Any(f => f.FullName == field.FullName && f.IsPublic))
                    errors.Add("Missing or inaccessible game field " + field.FullName);
            }
            foreach (var instruction in plugin.MainModule.GetTypes().SelectMany(t => t.Methods)
                .Where(m => m.HasBody).SelectMany(m => m.Body.Instructions))
            {
                if (!(instruction.Operand is FieldReference field) || field.DeclaringType.Scope.Name != "Assembly-CSharp"
                    || !types.TryGetValue(field.DeclaringType.FullName, out var owner)) continue;
                var definition = owner.Fields.FirstOrDefault(f => f.FullName == field.FullName);
                bool expectsStatic = instruction.OpCode.Code == Code.Ldsfld || instruction.OpCode.Code == Code.Ldsflda
                    || instruction.OpCode.Code == Code.Stsfld;
                if (definition != null && definition.IsStatic != expectsStatic)
                    errors.Add("Static/instance field contract changed for " + field.FullName);
            }
            // Harmony targets and injected private fields are not ordinary compiled member references.
            foreach (var target in new[] {
                new[] { "VoxelEngine.Brick", "LoadAllResourcesRuntime" },
                new[] { "Manager.ColliderShapeResources", "AllocateResources" },
                new[] { "Manager.GameResourcesManager", "OnResourceLoaded" },
                new[] { "VoxelEngine.VoxelMaterial", "CreateFaceIDArrayRuntime" },
                new[] { "VoxelEngine.VoxelItemCollider", "GetShapeFactor" },
                new[] { "VoxelEngine.EntitySymmetry", "ApplySymmetry" } })
            {
                var method = Method(types, target[0], target[1], errors);
                if (method == null) continue;
                string signature = method.ReturnType.FullName + "(" + string.Join(",", method.Parameters.Select(p => p.ParameterType.FullName)) + ")";
                string expected = target[1] == "CreateFaceIDArrayRuntime" ? "VoxelEngine.VoxelMaterial[](System.Object[])"
                    : target[1] == "GetShapeFactor" ? "System.Single(UnityEngine.Vector3)"
                    : target[1] == "ApplySymmetry" ? "System.Boolean(VoxelEngine.BrickInstanceData&,VoxelEngine.SymmetryPlane)"
                    : "System.Void()";
                if (signature != expected || method.HasThis != (target[1] == "GetShapeFactor"))
                    errors.Add("Patch signature changed for " + target[0] + "." + target[1]);
            }
            if (!types.TryGetValue("VoxelEngine.VoxelItemCollider", out var collider)
                || !collider.Fields.Any(f => f.Name == "path" && !f.IsStatic && f.FieldType.FullName == "System.String"))
                errors.Add("Missing injected VoxelItemCollider.path string field");
        }

        private static void ValidateMeshLoader(Dictionary<string, TypeDefinition> types, string type, string name, int expected, List<string> errors)
        {
            var method = Method(types, type, name, errors);
            if (method == null) return;
            if (name == ".ctor")
            {
                string parameters = string.Join(",", method.Parameters.Select(p => p.ParameterType.FullName));
                string required = type == "VoxelEngine.VoxelMesh"
                    ? "System.String,VoxelEngine.Brick,System.Single,VoxelEngine.ResizeMode,System.Boolean,System.Boolean"
                    : "VoxelEngine.VoxelMesh,Manager.ColliderShapeCommon/Shape,UnityEngine.Bounds,VoxelEngine.SubShape[]";
                if (parameters != required || method.IsStatic || !method.IsPublic)
                    errors.Add("Mesh-loader constructor signature changed for " + type);
            }
            int loads = method.Body.Instructions.Count(i => i.OpCode.Code == Code.Call
                && i.Operand is GenericInstanceMethod m && m.DeclaringType.FullName == "UnityEngine.Resources"
                && m.Name == "Load" && !m.HasThis && m.GenericArguments.Count == 1
                && m.GenericArguments[0].FullName == "UnityEngine.Mesh" && m.Parameters.Count == 1
                && m.Parameters[0].ParameterType.FullName == "System.String");
            if (loads != expected) errors.Add("Mesh-loader patch sites changed for " + type + "." + name);
        }

        private static MethodDefinition Method(Dictionary<string, TypeDefinition> types, string type, string name, List<string> errors)
        {
            if (types.TryGetValue(type, out var definition))
            {
                string parameters = name == ".ctor"
                    ? (type == "VoxelEngine.VoxelMesh" ? "System.String,VoxelEngine.Brick,System.Single,VoxelEngine.ResizeMode,System.Boolean,System.Boolean"
                        : "VoxelEngine.VoxelMesh,Manager.ColliderShapeCommon/Shape,UnityEngine.Bounds,VoxelEngine.SubShape[]")
                    : name == "CreateShape" ? "Manager.ColliderShapeCommon/Shape,System.String,Manager.ColliderShapeCommon/StretchMode,System.Boolean,Unity.Mathematics.float3"
                    : name == "CreateFaceIDArrayRuntime" ? "System.Object[]"
                    : name == "GetShapeFactor" ? "UnityEngine.Vector3"
                    : name == "ApplySymmetry" ? "VoxelEngine.BrickInstanceData&,VoxelEngine.SymmetryPlane" : "";
                var methods = definition.Methods.Where(m => m.Name == name && m.HasBody
                    && string.Join(",", m.Parameters.Select(p => p.ParameterType.FullName)) == parameters).ToArray();
                if (methods.Length == 1) return methods[0];
            }
            errors.Add("Missing or ambiguous patch method " + type + "." + name);
            return null;
        }

        private static int Constant(Instruction instruction)
        {
            if (instruction?.OpCode.Code == Code.Ldc_I4) return (int)instruction.Operand;
            if (instruction?.OpCode.Code == Code.Ldc_I4_S) return (sbyte)instruction.Operand;
            return -1;
        }
    }
}
