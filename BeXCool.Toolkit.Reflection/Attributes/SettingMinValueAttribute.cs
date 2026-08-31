using System;
using System.Collections.Generic;
using System.Text;

namespace BeXCool.Toolkit.Reflection
{
    [AttributeUsage(AttributeTargets.Property)]
    public class SettingMinValueAttribute : Attribute
    {
        public double Value { get; }

        public SettingMinValueAttribute(double value)
        {
            Value = value;
        }
    }
}
