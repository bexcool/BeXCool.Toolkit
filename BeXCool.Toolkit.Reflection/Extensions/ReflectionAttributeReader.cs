using BeXCool.Toolkit.Reflection;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace BeXCool.Toolkit.Reflection
{
    public static class ReflectionAttributeReader
    {
        private const BindingFlags MemberLookup =
            BindingFlags.Public | BindingFlags.NonPublic |
            BindingFlags.Static | BindingFlags.Instance |
            BindingFlags.FlattenHierarchy;

        /// <summary>
        /// Resolves the default value of <paramref name="propertyName"/> on <paramref name="obj"/>, in order:
        /// <list type="number">
        /// <item><see cref="SettingDefaultValueFromAttribute"/> - invokes the named member (method / property / field).</item>
        /// <item><see cref="SettingDefaultValueAttribute"/> - returns its constant; a <see cref="Type"/> value
        /// (i.e. <c>typeof(T)</c>) yields a fresh <see cref="Activator.CreateInstance(Type)"/>.</item>
        /// </list>
        /// Returns <see langword="null"/> when neither attribute is present or resolution fails.
        /// </summary>
        public static object? GetDefaultValue(this object obj, string propertyName)
        {
            if (obj is null || string.IsNullOrEmpty(propertyName))
                return null;

            var type = obj.GetType();
            var propInfo = type.GetProperty(propertyName);
            if (propInfo is null)
                return null;

            var from = propInfo.GetCustomAttribute<SettingDefaultValueFromAttribute>();
            if (from is not null)
                return ResolveMemberValue(obj, type, from.MemberName);

            var value = propInfo.GetCustomAttribute<SettingDefaultValueAttribute>()?.Value;

            if (value is Type factoryType)
                return Activator.CreateInstance(factoryType);

            return value;
        }

        /// <summary>
        /// <see langword="true"/> when <paramref name="propertyName"/> on <paramref name="obj"/>
        /// declares a default via <see cref="SettingDefaultValueAttribute"/> or
        /// <see cref="SettingDefaultValueFromAttribute"/>. Distinguishes "no default declared" from
        /// "the declared default is null".
        /// </summary>
        public static bool HasDefaultValue(this object? obj, string propertyName)
        {
            var propInfo = obj?.GetType().GetProperty(propertyName);

            return propInfo is not null
                && (propInfo.GetCustomAttribute<SettingDefaultValueFromAttribute>() is not null
                    || propInfo.GetCustomAttribute<SettingDefaultValueAttribute>() is not null);
        }

        public static double? GetMaxValue(this object obj, string propertyName)
        {
            var propInfo = obj.GetType().GetProperty(propertyName);

            return propInfo?.GetCustomAttribute<SettingMaxValueAttribute>()?.Value ?? null;
        }

        public static double? GetMinValue(this object obj, string propertyName)
        {
            var propInfo = obj.GetType().GetProperty(propertyName);

            return propInfo?.GetCustomAttribute<SettingMinValueAttribute>()?.Value ?? null;
        }

        private static object? ResolveMemberValue(object obj, Type type, string memberName)
        {
            if (string.IsNullOrEmpty(memberName))
                return null;

            var method = type.GetMethod(memberName, MemberLookup, binder: null, types: Type.EmptyTypes, modifiers: null);
            if (method is not null)
                return method.Invoke(method.IsStatic ? null : obj, null);

            var property = type.GetProperty(memberName, MemberLookup);
            if (property is not null && property.CanRead)
                return property.GetValue((property.GetMethod?.IsStatic ?? false) ? null : obj);

            var field = type.GetField(memberName, MemberLookup);
            if (field is not null)
                return field.GetValue(field.IsStatic ? null : obj);

            return null;
        }
    }
}
