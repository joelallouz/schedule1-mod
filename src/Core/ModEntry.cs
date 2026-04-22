using MelonLoader;
using ClientAssignmentOptimizer.Discovery;

[assembly: MelonInfo(typeof(ClientAssignmentOptimizer.Core.ModEntry), "Client Assignment Optimizer", "0.1.0", "joelallouz")]
[assembly: MelonGame("TVGS", "Schedule I")]

namespace ClientAssignmentOptimizer.Core
{
    public class ModEntry : MelonMod
    {
        private bool _runtimeVerificationDone = false;

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

        public override void OnSceneWasLoaded(int buildIndex, string sceneName)
        {
            ModLogger.Info($"Scene loaded: '{sceneName}' (index {buildIndex})");

            if (_runtimeVerificationDone || !ModConfig.DiscoveryEnabled)
                return;

            _runtimeVerificationDone = DiscoveryOrchestrator.RunRuntimeVerification();

            if (!_runtimeVerificationDone)
            {
                ModLogger.Info("Runtime verification deferred — data not available in this scene.");
            }
        }
    }
}
