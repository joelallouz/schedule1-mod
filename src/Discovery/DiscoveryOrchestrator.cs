using ClientAssignmentOptimizer.Core;

namespace ClientAssignmentOptimizer.Discovery
{
    public static class DiscoveryOrchestrator
    {
        public static void Run()
        {
            ModLogger.Info("=== Discovery Phase Starting ===");

            // Phase 0: What assemblies are loaded? How many types?
            RuntimeScanService.LogLoadedAssemblies();
            RuntimeScanService.LogGameAssemblyTypeCounts();

            // Phase 1: Search for client/dealer/assignment-related types
            TypeSearchService.SearchForClientRelatedTypes();

            ModLogger.Info("=== Discovery Phase Complete ===");
        }
    }
}
