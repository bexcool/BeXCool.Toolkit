using System;
using System.Collections.Generic;
using System.Text;

namespace BeXCool.Toolkit.Reflection
{
    [AttributeUsage(AttributeTargets.Property)]
    public class DefaultSettingValueAttribute : Attribute
    {
        public object Value { get; }

        public DefaultSettingValueAttribute(object value)
        {
            Value = value;
        }
    }
}
