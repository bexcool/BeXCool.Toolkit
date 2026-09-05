using System;
using System.Linq;
using System.Reflection;

namespace BeXCool.Toolkit.Reflection
{
    /// <summary>
    /// Generic "is this setting at its default?" / "reset this setting" helpers, driven by
    /// <see cref="SettingDefaultValueAttribute"/> / <see cref="SettingDefaultValueFromAttribute"/>.
    /// Reference-typed settings are compared by value and reset in place - no per-model boilerplate
    /// (no <c>Equals</c> override, no captured defaults), just the one attribute on the property.
    /// Public instance properties marked with <c>[JsonIgnore]</c> or <c>[SettingIgnore]</c> are
    /// treated as non-persisted and skipped by both compare and reset.
    /// </summary>
    public static class SettingsReflection
    {
        private const int MaxDepth = 8;
        private const BindingFlags PublicInstance = BindingFlags.Public | BindingFlags.Instance;

        /// <summary>
        /// <see langword="true"/> when the setting at <paramref name="path"/> equals its declared
        /// default. Also <see langword="true"/> when no default is declared or the path can't be
        /// resolved (i.e. "nothing to reset").
        /// </summary>
        public static bool IsAtDefault(this object? root, string path)
        {
            var (owner, prop) = ResolvePath(root, path);
            if (owner is null || prop is null || !prop.CanRead)
                return true;

            if (!TryResolveDefault(root, path, out var def))
                return true;

            return DeepEquals(prop.GetValue(owner), def, 0);
        }

        /// <summary>
        /// Resets the setting at <paramref name="path"/> to its declared default. Reference types are
        /// filled in place (so property setters with side-effects still run); value types / null are
        /// assigned. No-op when no default is declared.
        /// </summary>
        public static void ResetToDefault(this object? root, string path)
        {
            var (owner, prop) = ResolvePath(root, path);
            if (owner is null || prop is null)
                return;

            if (!TryResolveDefault(root, path, out var def))
                return;

            var current = prop.CanRead ? prop.GetValue(owner) : null;

            if (def is not null && current is not null
                && def.GetType() == current.GetType()
                && !IsSimple(def.GetType()))
            {
                CopyInto(def, current, 0);
            }
            else if (prop.CanWrite)
            {
                prop.SetValue(owner, def);
            }
        }

        /// <summary>
        /// Resolves the default value for a dotted <paramref name="path"/>. If an ancestor segment
        /// declares a default (e.g. a <see cref="SettingDefaultValueFromAttribute"/> factory on the
        /// parent object), the leaf's default is read off that default instance - so a factory that
        /// configures the whole object wins over a leaf's own <see cref="SettingDefaultValueAttribute"/>.
        /// Only when no ancestor declares a default does the leaf's own attribute apply.
        /// </summary>
        public static bool TryResolveDefault(object? root, string path, out object? value)
        {
            value = null;

            if (root is null || string.IsNullOrWhiteSpace(path))
                return false;

            var parts = path.Split('.');
            object? node = root;
            bool onDefaultInstance = false;

            for (int i = 0; i < parts.Length - 1; i++)
            {
                if (node is null) return false;

                if (!onDefaultInstance && node.HasDefaultValue(parts[i]))
                {
                    node = node.GetDefaultValue(parts[i]);
                    onDefaultInstance = true;
                }
                else
                {
                    var step = node.GetType().GetProperty(parts[i]);
                    if (step is null) return false;
                    node = step.GetValue(node);
                }
            }

            if (node is null) return false;

            var last = parts[^1];

            if (onDefaultInstance)
            {
                var leaf = node.GetType().GetProperty(last);
                if (leaf is null || !leaf.CanRead) return false;

                value = leaf.GetValue(node);
                return true;
            }

            if (!node.HasDefaultValue(last))
                return false;

            value = node.GetDefaultValue(last);
            return true;
        }

        /// <summary>
        /// Walks a dotted property path, returning the owner of the last segment and its
        /// <see cref="PropertyInfo"/>. Returns <c>(null, null)</c> if any segment is missing or null.
        /// </summary>
        public static (object? Owner, PropertyInfo? Property) ResolvePath(object? root, string path)
        {
            if (root is null || string.IsNullOrWhiteSpace(path))
                return (null, null);

            var parts = path.Split('.');
            var current = root;

            for (int i = 0; i < parts.Length - 1; i++)
            {
                var step = current?.GetType().GetProperty(parts[i]);
                if (step is null) return (null, null);

                current = step.GetValue(current);
                if (current is null) return (null, null);
            }

            return (current, current?.GetType().GetProperty(parts[^1]));
        }

        private static bool DeepEquals(object? a, object? b, int depth)
        {
            if (ReferenceEquals(a, b)) return true;
            if (a is null || b is null) return false;

            var type = a.GetType();
            if (type != b.GetType()) return false;

            if (IsSimple(type) || OverridesEquals(type) || depth >= MaxDepth)
                return a.Equals(b);

            foreach (var p in SettingProperties(type))
            {
                if (!p.CanRead) continue;
                if (!DeepEquals(p.GetValue(a), p.GetValue(b), depth + 1))
                    return false;
            }

            return true;
        }

        private static void CopyInto(object from, object to, int depth)
        {
            if (depth >= MaxDepth) return;

            foreach (var p in SettingProperties(to.GetType()))
            {
                if (!p.CanRead) continue;

                var value = p.GetValue(from);

                if (p.CanWrite)
                {
                    p.SetValue(to, value);
                    continue;
                }

                // Read-only reference property: fill its contents in place.
                var target = p.GetValue(to);
                if (value is not null && target is not null
                    && value.GetType() == target.GetType()
                    && !IsSimple(target.GetType()))
                {
                    CopyInto(value, target, depth + 1);
                }
            }
        }

        private static PropertyInfo[] SettingProperties(Type type)
            => type.GetProperties(PublicInstance)
                   .Where(p => p.GetIndexParameters().Length == 0 && !IsIgnored(p))
                   .ToArray();

        private static bool IsIgnored(PropertyInfo p)
            => p.GetCustomAttributes(true)
                .Any(a => a.GetType().Name is "JsonIgnoreAttribute" or "SettingIgnoreAttribute");

        private static bool OverridesEquals(Type type)
        {
            var m = type.GetMethod(nameof(Equals), new[] { typeof(object) });
            return m is not null && m.DeclaringType == type;
        }

        private static bool IsSimple(Type type)
        {
            type = Nullable.GetUnderlyingType(type) ?? type;
            return type.IsValueType || type == typeof(string);
        }
    }
}
