using System;
using System.Reflection;
using HarmonyLib;
using ClientAssignmentOptimizer.Core;
using ClientAssignmentOptimizer.UI;

namespace ClientAssignmentOptimizer.Patches
{
    /// <summary>
    /// Harmony patches on DealerManagementApp to inject the optimizer tab.
    /// Targets: SetOpen(bool), Refresh()
    /// </summary>
    public static class DealerAppPatches
    {
        private static Type _dealerAppType;
        private static bool _initialized;

        /// <summary>
        /// Apply all patches. Called from ModEntry after Harmony instance is created.
        /// </summary>
        public static void Apply(HarmonyLib.Harmony harmony)
        {
            _dealerAppType = FindType("Il2CppScheduleOne.UI.Phone.Messages.DealerManagementApp");
            if (_dealerAppType == null)
            {
                ModLogger.Warning("[DealerAppPatches] DealerManagementApp type not found — patches NOT applied.");
                return;
            }

            // Patch SetOpen(bool) — fires when app opens/closes
            var setOpen = _dealerAppType.GetMethod("SetOpen", new[] { typeof(bool) });
            if (setOpen == null)
            {
                // Try the Il2Cpp boolean type
                setOpen = _dealerAppType.GetMethod("SetOpen", BindingFlags.Public | BindingFlags.Instance);
            }

            if (setOpen != null)
            {
                var postfix = typeof(DealerAppPatches).GetMethod(nameof(SetOpen_Postfix),
                    BindingFlags.Static | BindingFlags.NonPublic);
                harmony.Patch(setOpen, postfix: new HarmonyMethod(postfix));
                ModLogger.Info("[DealerAppPatches] Patched DealerManagementApp.SetOpen");
            }
            else
            {
                ModLogger.Warning("[DealerAppPatches] SetOpen method not found on DealerManagementApp");
            }

            _initialized = true;
            ModLogger.Info("[DealerAppPatches] All patches applied successfully.");
        }

        /// <summary>
        /// Postfix for DealerManagementApp.SetOpen(bool open).
        /// When the app opens, we log and prepare to inject our UI.
        /// </summary>
        private static void SetOpen_Postfix(object __instance, bool open)
        {
            try
            {
                if (open)
                {
                    ModLogger.Info("[DealerAppPatches] DealerManagementApp OPENED.");
                    OptimizerTab.OnDealerAppOpened(__instance);
                }
                else
                {
                    ModLogger.Info("[DealerAppPatches] DealerManagementApp CLOSED.");
                    OptimizerTab.OnDealerAppClosed(__instance);
                }
            }
            catch (Exception ex)
            {
                ModLogger.Error($"[DealerAppPatches] SetOpen_Postfix error: {ex.Message}");
            }
        }

        private static Type FindType(string fullName)
        {
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                var type = asm.GetType(fullName);
                if (type != null) return type;
            }
            return null;
        }
    }
}
