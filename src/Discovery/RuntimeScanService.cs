using System;
using System.Linq;
using ClientAssignmentOptimizer.Core;

namespace ClientAssignmentOptimizer.Discovery
{
    public static class RuntimeScanService
    {
        /// <summary>
        /// Logs all loaded assemblies. Game-related assemblies are highlighted.
        /// Full list goes to Debug; summary goes to Info.
        /// </summary>
        public static void LogLoadedAssemblies()
        {
            ModLogger.Info("--- Loaded Assemblies ---");

            var assemblies = AppDomain.CurrentDomain.GetAssemblies();
            ModLogger.Info($"Total loaded assemblies: {assemblies.Length}");

            foreach (var asm in assemblies.OrderBy(a => a.GetName().Name))
            {
                ModLogger.Debug($"  {asm.GetName().Name} v{asm.GetName().Version}");
            }

            var gameAssemblies = assemblies
                .Where(a => ReflectionUtils.IsGameAssembly(a))
                .OrderBy(a => a.GetName().Name)
                .ToArray();

            ModLogger.Info($"Game-related assemblies found: {gameAssemblies.Length}");
            foreach (var asm in gameAssemblies)
            {
                ModLogger.Info($"  [GAME] {asm.GetName().Name}");
            }
        }

        /// <summary>
        /// For each game assembly, logs how many types it contains.
        /// Bounded and safe — does not enumerate type members.
        /// </summary>
        public static void LogGameAssemblyTypeCounts()
        {
            ModLogger.Info("--- Type Counts (Game Assemblies) ---");

            var gameAssemblies = AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => ReflectionUtils.IsGameAssembly(a))
                .OrderBy(a => a.GetName().Name);

            foreach (var asm in gameAssemblies)
            {
                try
                {
                    var types = ReflectionUtils.SafeGetTypes(asm);
                    ModLogger.Info($"  {asm.GetName().Name}: {types.Length} types");
                }
                catch (Exception ex)
                {
                    ModLogger.Warning($"  {asm.GetName().Name}: could not enumerate types — {ex.Message}");
                }
            }
        }
    }
}
