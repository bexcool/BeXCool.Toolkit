using BeXCool.Toolkit.Reflection;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace BeXCool.Toolkit.Reflection
{
    public static class ReflectionAttributeReader
    {
        public static object? GetDefaultValue(this object obj, string propertyName)
        {
            var propInfo = obj.GetType().GetProperty(propertyName);

            return propInfo?.GetCustomAttribute<DefaultSettingValueAttribute>()?.Value ?? null;
        }
    }
}
