using System;
using System.Collections.Generic;
using System.Linq;
using ClientAssignmentOptimizer.Core;

namespace ClientAssignmentOptimizer.Discovery
{
    /// <summary>
    /// Targeted type search across known game assemblies.
    /// Searches by keyword in type names, then dumps full shape for high-priority matches.
    /// READ-ONLY. No instantiation, no mutation.
    /// </summary>
    public static class TypeSearchService
    {
        /// <summary>
        /// Assembly name prefixes known to contain game logic.
        /// Discovered in Session 1: Assembly-CSharp (~3705 types), Il2CppScheduleOne.* (~46+ types).
        /// </summary>
        private static readonly string[] TargetAssemblyPrefixes =
        {
            "Assembly-CSharp",
            "Il2CppScheduleOne"
        };

        /// <summary>
        /// Keywords to search for in type names. Ordered by relevance to our mod.
        /// </summary>
        private static readonly string[] Keywords =
        {
            "Client", "Customer", "Dealer", "Assign",
            "Owner", "NPC", "Buyer", "Order",
            "Relationship", "Contact"
        };

        /// <summary>
        /// Types matching these keywords get a full DumpTypeShape.
        /// Others get a one-line summary only.
        /// </summary>
        private static readonly string[] FullDumpKeywords =
        {
            "Client", "Customer", "Dealer"
        };

        private const int MaxFullDumps = 15;

        public static void SearchForClientRelatedTypes()
        {
            ModLogger.Info("=== Targeted Type Search ===");
            ModLogger.Info($"  Keywords: {string.Join(", ", Keywords)}");

            var targetAssemblies = AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => IsTargetAssembly(a.GetName().Name ?? ""))
                .OrderBy(a => a.GetName().Name)
                .ToArray();

            ModLogger.Info($"  Scanning {targetAssemblies.Length} assemblies:");
            foreach (var asm in targetAssemblies)
            {
                var typeCount = ReflectionUtils.SafeGetTypes(asm).Length;
                ModLogger.Info($"    {asm.GetName().Name} ({typeCount} types)");
            }

            var matches = new List<TypeMatch>();

            foreach (var asm in targetAssemblies)
            {
                var types = ReflectionUtils.SafeGetTypes(asm);
                var asmName = asm.GetName().Name ?? "";

                foreach (var type in types)
                {
                    var typeName = type.Name ?? "";
                    var matchedKeyword = FindFirstKeywordMatch(typeName);
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

            // Summary
            ModLogger.Info($"  Total matching types: {matches.Count}");

            // Group by keyword for readable output
            ModLogger.Info("--- Matches by Keyword ---");
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

            // Full dumps for high-priority types
            var highPriority = matches
                .Where(m => IsFullDumpKeyword(m.Type.Name))
                .ToArray();

            if (highPriority.Length > 0)
            {
                var dumpCount = Math.Min(highPriority.Length, MaxFullDumps);
                ModLogger.Info($"--- Full Type Shapes ({dumpCount} of {highPriority.Length} high-priority types) ---");

                foreach (var match in highPriority.Take(MaxFullDumps))
                {
                    DumpUtils.DumpTypeShape(match.Type);
                }

                if (highPriority.Length > MaxFullDumps)
                {
                    ModLogger.Info($"  ... {highPriority.Length - MaxFullDumps} more high-priority types not shown (increase MaxFullDumps)");
                }
            }
            else
            {
                ModLogger.Warning("No high-priority types found (Client/Customer/Dealer). Assembly filter or keywords may need adjustment.");
            }

            ModLogger.Info("=== Targeted Type Search Complete ===");
        }

        private static bool IsTargetAssembly(string name)
        {
            return TargetAssemblyPrefixes.Any(prefix =>
                name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
        }

        private static string FindFirstKeywordMatch(string typeName)
        {
            foreach (var keyword in Keywords)
            {
                if (typeName.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0)
                    return keyword;
            }
            return null;
        }

        private static bool IsFullDumpKeyword(string typeName)
        {
            return FullDumpKeywords.Any(kw =>
                typeName.IndexOf(kw, StringComparison.OrdinalIgnoreCase) >= 0);
        }

        private struct TypeMatch
        {
            public Type Type;
            public string AssemblyName;
            public string Keyword;
        }
    }
}
