using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ClientAssignmentOptimizer.Core;

namespace ClientAssignmentOptimizer.Discovery
{
    /// <summary>
    /// Discovery scan for the in-game phone UI system.
    /// Goal: understand how phone apps (like DealerManagementApp) are registered,
    /// rendered, and navigated so we can inject our own app.
    /// READ-ONLY. No instantiation, no mutation.
    /// </summary>
    public static class PhoneUIDiscoveryService
    {
        private static readonly string[] TargetAssemblyPrefixes =
        {
            "Assembly-CSharp",
            "Il2CppScheduleOne"
        };

        /// <summary>
        /// Keywords to find phone/app framework types.
        /// </summary>
        private static readonly string[] PhoneKeywords =
        {
            "Phone", "App", "Screen", "Tab", "Menu",
            "Panel", "Page", "Window", "Dialog", "Popup",
            "Navigation", "UIManager"
        };

        /// <summary>
        /// Types matching these keywords get full shape dumps.
        /// </summary>
        private static readonly string[] FullDumpKeywords =
        {
            "Phone", "App"
        };

        private const int MaxFullDumps = 25;

        /// <summary>
        /// Phase 1: Search for all phone/app related types by keyword.
        /// </summary>
        public static void SearchPhoneTypes()
        {
            ModLogger.Info("=== Phone UI Discovery ===");

            var targetAssemblies = AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => IsTargetAssembly(a.GetName().Name ?? ""))
                .OrderBy(a => a.GetName().Name)
                .ToArray();

            var matches = new List<TypeMatch>();

            foreach (var asm in targetAssemblies)
            {
                var types = ReflectionUtils.SafeGetTypes(asm);
                var asmName = asm.GetName().Name ?? "";

                foreach (var type in types)
                {
                    var typeName = type.Name ?? "";
                    var fullName = type.FullName ?? "";
                    var ns = type.Namespace ?? "";

                    // Match on type name OR if the namespace contains "Phone" or "UI"
                    var matchedKeyword = FindFirstKeywordMatch(typeName);
                    if (matchedKeyword == null && (ns.Contains("Phone") || ns.Contains(".UI.")))
                    {
                        matchedKeyword = "Namespace:" + ns.Split('.').LastOrDefault(s => s == "Phone" || s == "UI");
                    }

                    if (matchedKeyword != null)
                    {
                        matches.Add(new TypeMatch
                        {
                            Type = type,
                            AssemblyName = asmName,
                            Keyword = matchedKeyword
                        });
                    }
                }
            }

            ModLogger.Info($"  Total phone/UI matching types: {matches.Count}");

            // Group by keyword
            ModLogger.Info("--- Phone/UI Matches by Keyword ---");
            var grouped = matches.GroupBy(m => m.Keyword).OrderBy(g => g.Key);
            foreach (var group in grouped)
            {
                ModLogger.Info($"  [{group.Key}] ({group.Count()} types):");
                foreach (var match in group.OrderBy(m => m.Type.FullName))
                {
                    var ns = match.Type.Namespace ?? "(no namespace)";
                    var baseType = match.Type.BaseType?.Name ?? "none";
                    ModLogger.Info($"    {ns}.{match.Type.Name} : {baseType} (in {match.AssemblyName})");
                }
            }

            ModLogger.Info("=== Phone UI Discovery Complete ===");
        }

        /// <summary>
        /// Phase 2: Deep dump of DealerManagementApp and its full inheritance chain.
        /// This is our reference implementation for how a phone app works.
        /// </summary>
        public static void DumpDealerManagementApp()
        {
            ModLogger.Info("=== DealerManagementApp Deep Dump ===");

            Type dealerAppType = null;
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (!IsTargetAssembly(asm.GetName().Name ?? "")) continue;
                foreach (var type in ReflectionUtils.SafeGetTypes(asm))
                {
                    if (type.Name == "DealerManagementApp")
                    {
                        dealerAppType = type;
                        break;
                    }
                }
                if (dealerAppType != null) break;
            }

            if (dealerAppType == null)
            {
                ModLogger.Warning("DealerManagementApp not found in any target assembly.");
                return;
            }

            // Full inheritance chain
            ModLogger.Info("--- Inheritance Chain ---");
            var chain = new List<Type>();
            var current = dealerAppType;
            while (current != null)
            {
                chain.Add(current);
                current = current.BaseType;
            }
            for (int i = 0; i < chain.Count; i++)
            {
                var indent = new string(' ', i * 2);
                ModLogger.Info($"  {indent}{chain[i].FullName ?? chain[i].Name}");
            }

            // Dump the app itself (up to 100 members for thoroughness)
            ModLogger.Info("--- DealerManagementApp Shape ---");
            DumpTypeExtended(dealerAppType, 100);

            // Dump each base class up to MonoBehaviour (skip Unity/IL2Cpp internals)
            foreach (var baseType in chain.Skip(1))
            {
                var baseName = baseType.FullName ?? baseType.Name;
                if (baseName.StartsWith("UnityEngine.") ||
                    baseName.StartsWith("Il2CppInterop.") ||
                    baseName.StartsWith("Il2CppSystem.") ||
                    baseName == "System.Object")
                    break;

                ModLogger.Info($"--- Base Class Shape: {baseName} ---");
                DumpTypeExtended(baseType, 100);
            }

            // Interfaces
            var interfaces = dealerAppType.GetInterfaces();
            if (interfaces.Length > 0)
            {
                ModLogger.Info($"--- Interfaces ({interfaces.Length}) ---");
                foreach (var iface in interfaces)
                {
                    ModLogger.Info($"  {iface.FullName ?? iface.Name}");
                }
            }

            ModLogger.Info("=== DealerManagementApp Deep Dump Complete ===");
        }

        /// <summary>
        /// Extended type dump with higher member limit and method parameters.
        /// </summary>
        private static void DumpTypeExtended(Type type, int maxMembers)
        {
            // Fields
            var fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly);
            ModLogger.Info($"  Fields (declared, {fields.Length}):");
            foreach (var field in fields.Take(maxMembers))
            {
                var mods = new List<string>();
                if (field.IsStatic) mods.Add("static");
                if (field.IsPublic) mods.Add("public");
                else if (field.IsPrivate) mods.Add("private");
                else if (field.IsFamily) mods.Add("protected");
                var label = mods.Count > 0 ? $"[{string.Join(" ", mods)}]" : "";
                ModLogger.Info($"    {field.FieldType.Name} {field.Name} {label}");
            }
            if (fields.Length > maxMembers)
                ModLogger.Info($"    ... and {fields.Length - maxMembers} more");

            // Properties
            var props = type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly);
            ModLogger.Info($"  Properties (declared, {props.Length}):");
            foreach (var prop in props.Take(maxMembers))
            {
                var accessors = new List<string>();
                if (prop.CanRead) accessors.Add("get");
                if (prop.CanWrite) accessors.Add("set");
                var isStatic = (prop.GetMethod?.IsStatic ?? false);
                var label = isStatic ? " [static]" : "";
                ModLogger.Info($"    {prop.PropertyType.Name} {prop.Name} {{ {string.Join("; ", accessors)} }}{label}");
            }
            if (props.Length > maxMembers)
                ModLogger.Info($"    ... and {props.Length - maxMembers} more");

            // Methods (declared only, including private — we need to see lifecycle hooks)
            var methods = type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly);
            ModLogger.Info($"  Methods (declared, {methods.Length}):");
            foreach (var method in methods.Take(maxMembers))
            {
                var parms = string.Join(", ", method.GetParameters().Select(p => $"{p.ParameterType.Name} {p.Name}"));
                var mods = new List<string>();
                if (method.IsStatic) mods.Add("static");
                if (method.IsVirtual) mods.Add("virtual");
                if (method.IsAbstract) mods.Add("abstract");
                var label = mods.Count > 0 ? $" [{string.Join(" ", mods)}]" : "";
                ModLogger.Info($"    {method.ReturnType.Name} {method.Name}({parms}){label}");
            }
            if (methods.Length > maxMembers)
                ModLogger.Info($"    ... and {methods.Length - maxMembers} more");
        }

        /// <summary>
        /// Phase 3: Dump specific types we need for the optimizer tab implementation.
        /// - EOrientation enum values
        /// - AppsCanvas (phone container)
        /// - CustomerSelector (reusable for our dropdown?)
        /// - The main Phone class (if it exists — controls open/close/orientation)
        /// </summary>
        public static void DumpPhoneInfrastructure()
        {
            ModLogger.Info("=== Phone Infrastructure Dump ===");

            var targetTypes = new[] { "EOrientation", "AppsCanvas", "CustomerSelector", "PlayerPhone", "PhoneController", "DropdownUI", "Phone", "HomeScreen", "PhoneShopInterface" };

            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (!IsTargetAssembly(asm.GetName().Name ?? "")) continue;
                foreach (var type in ReflectionUtils.SafeGetTypes(asm))
                {
                    var name = type.Name;
                    // Strip generic arity suffix for matching
                    var nameNoArity = name.Contains('`') ? name.Substring(0, name.IndexOf('`')) : name;

                    if (!Array.Exists(targetTypes, t => t == name || t == nameNoArity)) continue;

                    if (type.IsEnum)
                    {
                        ModLogger.Info($"--- Enum: {type.FullName} ---");
                        var values = Enum.GetNames(type);
                        for (int i = 0; i < values.Length; i++)
                        {
                            ModLogger.Info($"  {values[i]} = {(int)Enum.Parse(type, values[i])}");
                        }
                    }
                    else
                    {
                        ModLogger.Info($"--- {type.FullName} (base: {type.BaseType?.Name ?? "none"}) ---");
                        DumpTypeExtended(type, 100);
                    }
                }
            }

            // Also search for any class in the Phone namespace that looks like the phone controller
            // (might not be named "Phone" — could be the AppsCanvas namespace parent)
            ModLogger.Info("--- Searching for Phone controller class ---");
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (!IsTargetAssembly(asm.GetName().Name ?? "")) continue;
                foreach (var type in ReflectionUtils.SafeGetTypes(asm))
                {
                    var ns = type.Namespace ?? "";
                    var name = type.Name ?? "";
                    // Look for direct children of UI.Phone namespace that aren't nested/compiler-generated
                    if (ns == "Il2CppScheduleOne.UI.Phone" && !name.Contains("__") && !name.Contains("<") &&
                        !name.Contains("DisplayClass") && !name.Contains("CompilerGenerated") &&
                        !name.StartsWith("_") &&
                        (type.BaseType?.Name?.Contains("Singleton") == true ||
                         type.BaseType?.Name?.Contains("MonoBehaviour") == true))
                    {
                        ModLogger.Info($"  Phone controller candidate: {type.FullName} : {type.BaseType?.Name}");
                    }
                }
            }

            ModLogger.Info("=== Phone Infrastructure Dump Complete ===");
        }

        private static bool IsTargetAssembly(string name)
        {
            return TargetAssemblyPrefixes.Any(prefix =>
                name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
        }

        private static string FindFirstKeywordMatch(string typeName)
        {
            foreach (var keyword in PhoneKeywords)
            {
                if (typeName.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0)
                    return keyword;
            }
            return null;
        }

        private struct TypeMatch
        {
            public Type Type;
            public string AssemblyName;
            public string Keyword;
        }
    }
}
