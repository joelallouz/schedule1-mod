using MelonLoader;

namespace ClientAssignmentOptimizer.Core
{
    public static class ModConfig
    {
        private static MelonPreferences_Category _category;
        private static MelonPreferences_Entry<bool> _debugLogging;
        private static MelonPreferences_Entry<bool> _discoveryEnabled;
        private static MelonPreferences_Entry<int> _spendThreshold;
        private static MelonPreferences_Entry<bool> _enableFlagging;

        public static void Initialize()
        {
            _category = MelonPreferences.CreateCategory("ClientOptimizer");
            _debugLogging     = _category.CreateEntry("DebugLogging",     true,  "Verbose debug logging");
            _discoveryEnabled = _category.CreateEntry("DiscoveryEnabled", false, "Run discovery scans on startup");
            _spendThreshold   = _category.CreateEntry("SpendThreshold",   700,   "Flag dealer customers with MaxWeeklySpend above this");
            _enableFlagging   = _category.CreateEntry("EnableFlagging",   true,  "Show the flag column and amber row tint for high-value dealer customers");
        }

        public static bool DebugLogging
        {
            get => _debugLogging?.Value ?? true;
            set { if (_debugLogging != null) _debugLogging.Value = value; }
        }

        public static bool DiscoveryEnabled
        {
            get => _discoveryEnabled?.Value ?? false;
            set { if (_discoveryEnabled != null) _discoveryEnabled.Value = value; }
        }

        public static int SpendThreshold
        {
            get => _spendThreshold?.Value ?? 700;
            set
            {
                if (_spendThreshold != null)
                {
                    _spendThreshold.Value = value;
                    _category?.SaveToFile(false);
                }
            }
        }

        public static bool EnableFlagging
        {
            get => _enableFlagging?.Value ?? true;
            set
            {
                if (_enableFlagging != null)
                {
                    _enableFlagging.Value = value;
                    _category?.SaveToFile(false);
                }
            }
        }
    }
}
