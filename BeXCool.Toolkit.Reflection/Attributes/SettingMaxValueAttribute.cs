using System;
using System.Collections.Generic;
using System.Text;

namespace BeXCool.Toolkit.Reflection
{
    [AttributeUsage(AttributeTargets.Property)]
    public class SettingMaxValueAttribute : Attribute
    {
        public double Value { get; }

        public SettingMaxValueAttribute(double value)
        {
            Value = value;
        }
    }
}
