using System.Diagnostics;
using MelonLoader;
using UnityEngine;
using ClientAssignmentOptimizer.Discovery;
using ClientAssignmentOptimizer.UI;

[assembly: MelonInfo(typeof(ClientAssignmentOptimizer.Core.ModEntry), "Client Assignment Optimizer", "0.1.0", "joelallouz")]
[assembly: MelonGame("TVGS", "Schedule I")]

namespace ClientAssignmentOptimizer.Core
{
    public class ModEntry : MelonMod
    {
        private bool _runtimeVerificationDone = false;
        private Stopwatch _mainSceneTimer;
        private bool _inMainScene = false;

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

            ModLogger.Info("Initialization complete. Press F9 in-game to open customer panel.");
        }

        public override void OnSceneWasLoaded(int buildIndex, string sceneName)
        {
            ModLogger.Info($"Scene loaded: '{sceneName}' (index {buildIndex})");

            _inMainScene = sceneName == "Main";

            if (!_inMainScene && CustomerPanelUI.Visible)
            {
                CustomerPanelUI.Toggle();
                SetGamePaused(false);
                ModLogger.Info("Left Main scene — closing customer panel.");
            }

            if (_inMainScene && !_runtimeVerificationDone && ModConfig.DiscoveryEnabled)
            {
                _mainSceneTimer = Stopwatch.StartNew();
                ModLogger.Info($"Main scene detected — runtime verification will run in {VerificationDelaySeconds}s");
            }
        }

        public override void OnUpdate()
        {
            // Delayed runtime verification
            if (_mainSceneTimer != null && !_runtimeVerificationDone)
            {
                if (_mainSceneTimer.Elapsed.TotalSeconds >= VerificationDelaySeconds)
                {
                    _mainSceneTimer = null;
                    ModLogger.Info("Delay elapsed — running runtime verification now.");
                    _runtimeVerificationDone = DiscoveryOrchestrator.RunRuntimeVerification();

                    if (!_runtimeVerificationDone)
                    {
                        ModLogger.Warning("Runtime verification found no data after delay.");
                    }
                }
            }

            // Hotkey: F9 toggles panel (and pauses/unpauses game)
            if (_inMainScene && Input.GetKeyDown(KeyCode.F9))
            {
                CustomerPanelUI.Toggle();
                SetGamePaused(CustomerPanelUI.Visible);
                ModLogger.Info($"Customer panel: {(CustomerPanelUI.Visible ? "OPEN (game paused)" : "CLOSED (game resumed)")}");
            }

            // Hotkey: F10 refreshes data while panel is open
            if (_inMainScene && CustomerPanelUI.Visible && Input.GetKeyDown(KeyCode.F10))
            {
                CustomerPanelUI.Refresh();
                ModLogger.Info("Customer panel data refreshed.");
            }

            // Keep cursor unlocked while panel is open (game fights to re-lock every frame)
            if (CustomerPanelUI.Visible)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }

        public override void OnGUI()
        {
            CustomerPanelUI.Draw();
        }

        private static void SetGamePaused(bool paused)
        {
            Time.timeScale = paused ? 0f : 1f;
        }
    }
}
