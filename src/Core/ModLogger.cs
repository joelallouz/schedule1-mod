using MelonLoader;

namespace ClientAssignmentOptimizer.Core
{
    public static class ModLogger
    {
        private const string Prefix = "[ClientOptimizer]";

        public static void Info(string message)
        {
            MelonLogger.Msg($"{Prefix} {message}");
        }

        public static void Warning(string message)
        {
            MelonLogger.Warning($"{Prefix} {message}");
        }

        public static void Error(string message)
        {
            MelonLogger.Error($"{Prefix} {message}");
        }

        public static void Debug(string message)
        {
            if (ModConfig.DebugLogging)
            {
                MelonLogger.Msg($"{Prefix} [DEBUG] {message}");
            }
        }
    }
}
