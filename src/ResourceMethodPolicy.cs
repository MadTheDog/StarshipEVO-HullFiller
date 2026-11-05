using System;
using System.Reflection;
namespace HullFiller
{
    public static class ResourceMethodPolicy
    {
        public static bool IsTypedLoad(MethodInfo method, Type resourceType, Type assetType)
        {
            if (method == null || method.DeclaringType != resourceType || method.Name != "Load"
                || !method.IsGenericMethod || method.ContainsGenericParameters) return false;
            var generic = method.GetGenericArguments();
            var parameters = method.GetParameters();
            return generic.Length == 1 && generic[0] == assetType
                && parameters.Length == 1 && parameters[0].ParameterType == typeof(string);
        }
    }
}
