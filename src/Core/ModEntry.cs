using System.Diagnostics;
using MelonLoader;
using ClientAssignmentOptimizer.Discovery;

[assembly: MelonInfo(typeof(ClientAssignmentOptimizer.Core.ModEntry), "Client Assignment Optimizer", "0.1.0", "joelallouz")]
[assembly: MelonGame("TVGS", "Schedule I")]

namespace ClientAssignmentOptimizer.Core
{
    public class ModEntry : MelonMod
    {
        private bool _runtimeVerificationDone = false;
        private Stopwatch _mainSceneTimer;

        private const double VerificationDelaySeconds = 10.0;

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

            if (sceneName == "Main")
            {
                _mainSceneTimer = Stopwatch.StartNew();
                ModLogger.Info($"Main scene detected — runtime verification will run in {VerificationDelaySeconds}s");
            }
        }

        public override void OnUpdate()
        {
            if (_mainSceneTimer == null || _runtimeVerificationDone)
                return;

            if (_mainSceneTimer.Elapsed.TotalSeconds >= VerificationDelaySeconds)
            {
                _mainSceneTimer = null;
                ModLogger.Info($"Delay elapsed — running runtime verification now.");
                _runtimeVerificationDone = DiscoveryOrchestrator.RunRuntimeVerification();

                if (!_runtimeVerificationDone)
                {
                    ModLogger.Warning("Runtime verification found no data after delay.");
                }
            }
        }
    }
}
