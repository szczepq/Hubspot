using System;
using System.Reflection;

namespace SoftwareHut.HubspotService.Test.ArgumentSpecimens
{
    public static class ReflectionHelper
    {
        public static bool IsParameterDeclaredInCtor<TTarget, TParameterType>(this object target, string paramName)
        {
            return target is ParameterInfo parameter &&
                   parameter.Member.DeclaringType == typeof(TTarget) &&
                   parameter.Member.MemberType == MemberTypes.Constructor &&
                   parameter.ParameterType == typeof(TParameterType) &&
                   parameter.Name.Equals(paramName, StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsPropertyDeclaredIn<TTarget, TParameterType>(this object target, string paramName)
        {
            return target is ParameterInfo parameter &&
                   parameter.Member.DeclaringType == typeof(TTarget) &&
                   parameter.Member.MemberType == MemberTypes.Property &&
                   parameter.ParameterType == typeof(TParameterType) &&
                   parameter.Name.Equals(paramName, StringComparison.OrdinalIgnoreCase);
        }

        public static bool IsFieldDeclaredIn<TTarget, TParameterType>(this object target, string paramName)
        {
            return target is ParameterInfo parameter &&
                   parameter.Member.DeclaringType == typeof(TTarget) &&
                   parameter.Member.MemberType == MemberTypes.Field &&
                   parameter.ParameterType == typeof(TParameterType) &&
                   parameter.Name.Equals(paramName, StringComparison.OrdinalIgnoreCase);
        }
    }
}