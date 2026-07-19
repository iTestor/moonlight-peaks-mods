using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace ManaPlus
{
    /// <summary>
    /// Shared reflection helpers used across all Harmony patches in this plugin.
    /// EntitySpawner/SpawnConfig/RespawnRarity types are only ever accessed via reflection
    /// (no compile-time reference to the game assembly), so every patch needs the same small
    /// set of "find field/property in type hierarchy" utilities. Centralized here so the
    /// lookup logic exists exactly once instead of being copy-pasted into every patch file.
    /// </summary>
    internal static class ReflectionHelpers
    {
        internal const BindingFlags InstanceFlags =
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

        /// <summary>Walks up the type hierarchy to find an instance field (public or non-public) by name.</summary>
        internal static FieldInfo FindFieldInHierarchy(Type type, string fieldName)
        {
            Type current = type;
            while (current != null)
            {
                FieldInfo field = current.GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                if (field != null)
                    return field;
                current = current.BaseType;
            }
            return null;
        }

        /// <summary>Looks up an instance property (public or non-public) by name.</summary>
        internal static PropertyInfo GetPropertyInfo(object obj, string propName)
        {
            return obj?.GetType().GetProperty(propName, InstanceFlags);
        }

        /// <summary>Reads a property value. Returns null if the object or the property doesn't exist.</summary>
        internal static object GetPropertyValue(object obj, string propName)
        {
            return GetPropertyInfo(obj, propName)?.GetValue(obj);
        }

        /// <summary>Reads a member value, trying a property first and falling back to a field of the same name.</summary>
        internal static object GetMemberValue(object obj, string memberName)
        {
            if (obj == null) return null;
            Type type = obj.GetType();

            PropertyInfo prop = type.GetProperty(memberName, InstanceFlags);
            if (prop != null) return prop.GetValue(obj);

            FieldInfo field = type.GetField(memberName, InstanceFlags);
            return field?.GetValue(obj);
        }

        /// <summary>Writes a member value, trying a property first and falling back to a field, converting the value to the target type.</summary>
        internal static void SetMemberValue(object obj, string memberName, object value)
        {
            if (obj == null) return;
            Type type = obj.GetType();

            PropertyInfo prop = type.GetProperty(memberName, InstanceFlags);
            if (prop != null && prop.CanWrite)
            {
                prop.SetValue(obj, Convert.ChangeType(value, prop.PropertyType));
                return;
            }

            FieldInfo field = type.GetField(memberName, InstanceFlags);
            if (field != null)
            {
                field.SetValue(obj, Convert.ChangeType(value, field.FieldType));
            }
        }

        /// <summary>Returns the Count of a field's value if that value is an ICollection, otherwise 0.</summary>
        internal static int GetCollectionCount(object obj, string fieldName)
        {
            if (obj == null) return 0;
            FieldInfo field = obj.GetType().GetField(fieldName, InstanceFlags);
            return field?.GetValue(obj) is ICollection collection ? collection.Count : 0;
        }

        /// <summary>
        /// Returns all instance methods with the given name that take exactly one parameter.
        /// Needed for overloaded methods (e.g. "FindOrCreate") where GetMethod(string) would
        /// throw an AmbiguousMatchException.
        /// </summary>
        internal static MethodInfo[] FindSingleArgMethodOverloads(Type type, string methodName)
        {
            return type.GetMethods(InstanceFlags)
                .Where(m => m.Name == methodName && m.GetParameters().Length == 1)
                .ToArray();
        }

        /// <summary>Returns the first single-argument method with the given name, or null if none exists.</summary>
        internal static MethodInfo FindSingleArgMethod(Type type, string methodName)
        {
            return FindSingleArgMethodOverloads(type, methodName).FirstOrDefault();
        }

        /// <summary>Picks the overload (from a set of single-argument candidates) whose parameter type matches the runtime type of the given argument.</summary>
        internal static MethodInfo FindMatchingOverload(MethodInfo[] overloads, object argument)
        {
            return overloads.FirstOrDefault(m => m.GetParameters()[0].ParameterType.IsInstanceOfType(argument));
        }
    }

    /// <summary>
    /// Reference-identity equality comparer. Used to track already-patched ScriptableObject
    /// instances by identity rather than by value equality.
    /// </summary>
    internal sealed class ReferenceEqualityComparer : IEqualityComparer<object>
    {
        internal static readonly ReferenceEqualityComparer Instance = new ReferenceEqualityComparer();
        bool IEqualityComparer<object>.Equals(object x, object y) => ReferenceEquals(x, y);
        int IEqualityComparer<object>.GetHashCode(object obj) => System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj);
    }
}