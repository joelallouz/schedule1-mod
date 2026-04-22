using ClientAssignmentOptimizer.Core;

namespace ClientAssignmentOptimizer.Discovery
{
    public static class DiscoveryOrchestrator
    {
        public static void Run()
        {
            ModLogger.Info("=== Discovery Phase Starting ===");

            RuntimeScanService.LogLoadedAssemblies();
            RuntimeScanService.LogGameAssemblyTypeCounts();

            ModLogger.Info("=== Discovery Phase Complete ===");
        }
    }
}
