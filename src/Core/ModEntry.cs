using MelonLoader;
using ClientAssignmentOptimizer.Discovery;

[assembly: MelonInfo(typeof(ClientAssignmentOptimizer.Core.ModEntry), "Client Assignment Optimizer", "0.1.0", "joelallouz")]
[assembly: MelonGame("TVGS", "Schedule I")]

namespace ClientAssignmentOptimizer.Core
{
    public class ModEntry : MelonMod
    {
        public override void OnInitializeMelon()
        {
            ModLogger.Info("========================================");
            ModLogger.Info("Client Assignment Optimizer v0.1.0");
            ModLogger.Info("========================================");

            ModLogger.Info($"Debug logging: {(ModConfig.DebugLogging ? "ON" : "OFF")}");
            ModLogger.Info($"Discovery mode: {(ModConfig.DiscoveryEnabled ? "ON" : "OFF")}");

            if (ModConfig.DiscoveryEnabled)
            {
                DiscoveryOrchestrator.Run();
            }

            ModLogger.Info("Initialization complete.");
        }
    }
}
