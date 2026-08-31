using System;
using System.Collections.Generic;
using System.Text;

namespace BeXCool.Toolkit.Reflection
{
    [AttributeUsage(AttributeTargets.Property)]
    public class SettingDefaultValueAttribute : Attribute
    {
        public object Value { get; }

        public SettingDefaultValueAttribute(object value)
        {
            Value = value;
        }
    }
}
