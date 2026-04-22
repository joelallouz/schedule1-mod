using System;
using System.Linq;
using System.Reflection;

namespace ClientAssignmentOptimizer.Discovery
{
    public static class ReflectionUtils
    {
        /// <summary>
        /// Prefixes that indicate framework/engine assemblies (not game code).
        /// Intentionally conservative — we'd rather show a few extra assemblies
        /// than accidentally hide game code. Tune as findings come in.
        /// </summary>
        private static readonly string[] FrameworkPrefixes =
        {
            "System", "Microsoft", "Mono", "mscorlib", "netstandard",
            "MelonLoader", "Harmony", "HarmonyLib",
            "UnityEngine", "Unity.",
            "Il2CppInterop", "Il2Cppmscorlib", "Il2CppSystem", "Il2CppMono",
            "Newtonsoft", "Tomlet"
        };

        /// <summary>
        /// Returns true if the assembly is likely game code (not engine/framework).
        /// </summary>
        public static bool IsGameAssembly(Assembly assembly)
        {
            var name = assembly.GetName().Name ?? "";
            if (string.IsNullOrEmpty(name)) return false;

            return !FrameworkPrefixes.Any(prefix =>
                name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// GetTypes() that doesn't throw on partially-loadable assemblies.
        /// </summary>
        public static Type[] SafeGetTypes(Assembly assembly)
        {
            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                return ex.Types.Where(t => t != null).ToArray();
            }
        }

        /// <summary>
        /// Readable type name including namespace.
        /// </summary>
        public static string FormatType(Type type)
        {
            return $"{type.Namespace ?? "(no namespace)"}.{type.Name}";
        }
    }
}
