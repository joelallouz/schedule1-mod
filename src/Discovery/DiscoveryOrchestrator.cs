using ClientAssignmentOptimizer.Core;

namespace ClientAssignmentOptimizer.Discovery
{
    public static class DiscoveryOrchestrator
    {
        /// <summary>
        /// Reflection-only discovery: type scanning, keyword search, NPC dump, method search.
        /// Runs during OnInitializeMelon — does not need live game data.
        /// </summary>
        public static void Run()
        {
            ModLogger.Info("=== Discovery Phase Starting ===");

            // Phase 0: What assemblies are loaded? How many types?
            RuntimeScanService.LogLoadedAssemblies();
            RuntimeScanService.LogGameAssemblyTypeCounts();

            // Phase 1: Search for client/dealer/assignment-related types
            TypeSearchService.SearchForClientRelatedTypes();

            // Session 3: NPC base class shape + assignment method search
            NPCTypeScanService.DumpNPCBaseClass();
            NPCTypeScanService.SearchAssignmentMethods();

            ModLogger.Info("=== Discovery Phase Complete ===");
        }

        /// <summary>
        /// Runtime verification: reads live Customer/Dealer data.
        /// Must run AFTER a save game is loaded (called from OnSceneWasLoaded).
        /// Returns true if data was accessible, false if game data isn't loaded yet.
        /// </summary>
        public static bool RunRuntimeVerification()
        {
            return RuntimeVerificationService.Verify();
        }
    }
}
