using System;

namespace BeXCool.Toolkit.Reflection
{
    /// <summary>
    /// Marks a setting property whose default value is produced by a member on the declaring type,
    /// for defaults that cannot be a compile-time constant (structs, reference types, computed or
    /// configured values).
    /// <para>
    /// The referenced member may be a static or instance parameterless method, a readable property,
    /// or a field. For reference types it should return a fresh instance on every call so the
    /// "default" is never the same mutable object that is live in the settings.
    /// </para>
    /// <example><code>
    /// [SettingDefaultValueFrom(nameof(DefaultValueFont))]
    /// public WidgetFont ValueFont { get => _valueFont; set => SetProperty(ref _valueFont, value); }
    ///
    /// public static WidgetFont DefaultValueFont() => new() { Size = 50 };
    /// </code></example>
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class SettingDefaultValueFromAttribute : Attribute
    {
        /// <summary>Name of the method / property / field on the declaring type that yields the default value.</summary>
        public string MemberName { get; }

        public SettingDefaultValueFromAttribute(string memberName)
        {
            MemberName = memberName;
        }
    }
}
