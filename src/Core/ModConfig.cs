namespace ClientAssignmentOptimizer.Core
{
    public static class ModConfig
    {
        /// <summary>
        /// When true, ModLogger.Debug() calls produce output.
        /// Set to false to reduce log noise once discovery is complete.
        /// </summary>
        public static bool DebugLogging { get; set; } = true;

        /// <summary>
        /// When true, DiscoveryOrchestrator runs on startup.
        /// Disable once discovery phase is complete to skip scan overhead.
        /// </summary>
        public static bool DiscoveryEnabled { get; set; } = true;
    }
}
