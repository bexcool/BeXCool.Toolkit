using System;
using System.Collections.Generic;
using System.Text;

namespace BeXCool.Toolkit.Reflection.Attributes
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
