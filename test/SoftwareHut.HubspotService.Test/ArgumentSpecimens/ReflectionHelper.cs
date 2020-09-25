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

        public static bool MatchesConstructorArgument(this object request,Type declaringType, string targetName, out Type targetType)
        {
            if (request is ParameterInfo parameterInfo &&
                parameterInfo.Member.DeclaringType == declaringType &&
                parameterInfo.Member.MemberType == MemberTypes.Constructor &&
                parameterInfo.Name.Equals(targetName, StringComparison.OrdinalIgnoreCase))
            {
                targetType = parameterInfo.ParameterType;
                return true;
            }

            targetType = null;
            return false;
        }

        public static bool MatchesProperty(this object request, Type declaringType, string targetName, out Type targetType)
        {
            if (request is PropertyInfo propertyInfo &&
                propertyInfo.DeclaringType == declaringType &&
                propertyInfo.MemberType == MemberTypes.Property &&
                propertyInfo.Name.Equals(targetName, StringComparison.OrdinalIgnoreCase))
            {
                targetType = propertyInfo.PropertyType;
                return true;
            }

            targetType = null;
            return false;
        }

        public static bool MatchesField(this object request, Type declaringType, string targetName, out Type targetType)
        {
            if (request is FieldInfo fieldInfo &&
                fieldInfo.DeclaringType == declaringType &&
                fieldInfo.MemberType == MemberTypes.Field &&
                fieldInfo.Name.Equals(targetName, StringComparison.OrdinalIgnoreCase))
            {
                targetType = fieldInfo.FieldType;
                return true;
            }

            targetType = null;
            return false;
        }
    }
}