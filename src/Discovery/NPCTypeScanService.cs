using System;
using System.Linq;
using System.Reflection;
using ClientAssignmentOptimizer.Core;

namespace ClientAssignmentOptimizer.Discovery
{
    /// <summary>
    /// Session 3: Dump NPC base class shape and search Dealer/Customer for
    /// assignment-related methods. Reflection-only — no live data needed.
    /// </summary>
    public static class NPCTypeScanService
    {
        private static readonly string[] AssignMethodKeywords =
        {
            "Assign", "Remove", "Add", "Customer", "Transfer", "Unassign"
        };

        private static readonly string[] NamePropertyKeywords =
        {
            "Name", "Id", "ID", "Label", "Display", "Title", "First", "Last"
        };

        private const int MaxProperties = 100;

        /// <summary>
        /// Dumps the NPC base class with focus on name/ID fields.
        /// Dealer extends NPC; Customer has-a NPC reference.
        /// </summary>
        public static void DumpNPCBaseClass()
        {
            ModLogger.Info("=== NPC Base Class Dump ===");

            var npcType = FindType("Il2CppScheduleOne.NPCs.NPC");
            if (npcType == null)
            {
                ModLogger.Warning("Could not find Il2CppScheduleOne.NPCs.NPC type");
                ModLogger.Info("=== NPC Base Class Dump: FAILED ===");
                return;
            }

            ModLogger.Info($"Type: {npcType.FullName}");
            ModLogger.Info($"Base: {npcType.BaseType?.FullName ?? "none"}");

            // Walk the inheritance chain
            ModLogger.Info("Inheritance chain:");
            var current = npcType;
            while (current != null)
            {
                ModLogger.Info($"  {current.FullName}");
                current = current.BaseType;
            }

            var props = npcType.GetProperties(
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.FlattenHierarchy);

            // Highlight name/ID properties
            var nameProps = props
                .Where(p => NamePropertyKeywords.Any(kw =>
                    p.Name.IndexOf(kw, StringComparison.OrdinalIgnoreCase) >= 0))
                .ToArray();

            ModLogger.Info($"Name/ID-related properties ({nameProps.Length}):");
            foreach (var prop in nameProps)
            {
                var label = (prop.GetMethod?.IsStatic ?? false) ? " [static]" : "";
                ModLogger.Info($"  {prop.PropertyType.Name} {prop.Name}{label}");
            }

            // Full property list (bounded)
            ModLogger.Info($"All public properties ({props.Length}, showing up to {MaxProperties}):");
            foreach (var prop in props.Take(MaxProperties))
            {
                var label = (prop.GetMethod?.IsStatic ?? false) ? " [static]" : "";
                ModLogger.Info($"  {prop.PropertyType.Name} {prop.Name}{label}");
            }
            if (props.Length > MaxProperties)
                ModLogger.Info($"  ... and {props.Length - MaxProperties} more");

            ModLogger.Info("=== NPC Base Class Dump Complete ===");
        }

        /// <summary>
        /// Searches Dealer and Customer for methods related to assignment.
        /// Looks for: Assign, Remove, Add, Customer, Transfer, Unassign.
        /// Logs full signatures including declaring type.
        /// </summary>
        public static void SearchAssignmentMethods()
        {
            ModLogger.Info("=== Assignment Method Search ===");

            SearchMethodsOnType("Il2CppScheduleOne.Economy.Dealer", "Dealer");
            SearchMethodsOnType("Il2CppScheduleOne.Economy.Customer", "Customer");

            ModLogger.Info("=== Assignment Method Search Complete ===");
        }

        private static void SearchMethodsOnType(string typeName, string label)
        {
            var type = FindType(typeName);
            if (type == null)
            {
                ModLogger.Warning($"Could not find {typeName}");
                return;
            }

            // Get ALL public methods (instance + static, declared + inherited)
            var allMethods = type.GetMethods(
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.FlattenHierarchy);

            var matches = allMethods
                .Where(m => AssignMethodKeywords.Any(kw =>
                    m.Name.IndexOf(kw, StringComparison.OrdinalIgnoreCase) >= 0))
                .OrderBy(m => m.Name)
                .ToArray();

            ModLogger.Info($"{label}: {allMethods.Length} total public methods, {matches.Length} match assignment keywords");

            foreach (var method in matches)
            {
                var parms = string.Join(", ",
                    method.GetParameters().Select(p => $"{p.ParameterType.Name} {p.Name}"));
                var staticLabel = method.IsStatic ? " [static]" : "";
                var declaring = method.DeclaringType?.Name ?? "?";
                ModLogger.Info($"  {method.ReturnType.Name} {method.Name}({parms}){staticLabel} — from {declaring}");
            }
        }

        private static Type FindType(string fullName)
        {
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                var type = asm.GetType(fullName);
                if (type != null) return type;
            }
            return null;
        }
    }
}
