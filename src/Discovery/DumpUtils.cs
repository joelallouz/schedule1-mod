using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ClientAssignmentOptimizer.Core;

namespace ClientAssignmentOptimizer.Discovery
{
    /// <summary>
    /// Utilities for structured logging of type shapes and instance values.
    /// All methods are READ-ONLY — they never mutate game state.
    /// All output is BOUNDED — large member lists are truncated.
    /// </summary>
    public static class DumpUtils
    {
        private const int MaxMembers = 50;

        /// <summary>
        /// Logs the shape of a type: fields, properties, and declared methods.
        /// Does not instantiate anything or read values.
        /// </summary>
        public static void DumpTypeShape(Type type)
        {
            ModLogger.Info($"--- Type Shape: {ReflectionUtils.FormatType(type)} ---");
            ModLogger.Info($"  Base type: {type.BaseType?.Name ?? "none"}");

            var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);
            ModLogger.Info($"  Public fields ({fields.Length}):");
            foreach (var field in fields.Take(MaxMembers))
            {
                var label = field.IsStatic ? " [static]" : "";
                ModLogger.Info($"    {field.FieldType.Name} {field.Name}{label}");
            }
            if (fields.Length > MaxMembers)
                ModLogger.Info($"    ... and {fields.Length - MaxMembers} more");

            var props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);
            ModLogger.Info($"  Public properties ({props.Length}):");
            foreach (var prop in props.Take(MaxMembers))
            {
                var accessors = new List<string>();
                if (prop.CanRead) accessors.Add("get");
                if (prop.CanWrite) accessors.Add("set");
                var label = (prop.GetMethod?.IsStatic ?? false) ? " [static]" : "";
                ModLogger.Info($"    {prop.PropertyType.Name} {prop.Name} {{ {string.Join("; ", accessors)} }}{label}");
            }
            if (props.Length > MaxMembers)
                ModLogger.Info($"    ... and {props.Length - MaxMembers} more");

            var methods = type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly);
            ModLogger.Info($"  Public instance methods (declared, {methods.Length}):");
            foreach (var method in methods.Take(MaxMembers))
            {
                var parms = string.Join(", ", method.GetParameters().Select(p => $"{p.ParameterType.Name} {p.Name}"));
                ModLogger.Info($"    {method.ReturnType.Name} {method.Name}({parms})");
            }
            if (methods.Length > MaxMembers)
                ModLogger.Info($"    ... and {methods.Length - MaxMembers} more");
        }

        /// <summary>
        /// Reads and logs public instance field values from a live object.
        /// READ-ONLY — never sets any values.
        /// </summary>
        public static void DumpInstanceValues(object instance)
        {
            if (instance == null)
            {
                ModLogger.Warning("DumpInstanceValues called with null");
                return;
            }

            var type = instance.GetType();
            ModLogger.Info($"--- Instance Values: {ReflectionUtils.FormatType(type)} ---");

            var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance);
            foreach (var field in fields.Take(MaxMembers))
            {
                try
                {
                    var value = field.GetValue(instance);
                    ModLogger.Info($"    {field.Name} = {value ?? "null"}");
                }
                catch (Exception ex)
                {
                    ModLogger.Debug($"    {field.Name} = <error: {ex.Message}>");
                }
            }
        }
    }
}
