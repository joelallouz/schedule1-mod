using System;
using System.Reflection;
using ClientAssignmentOptimizer.Core;

namespace ClientAssignmentOptimizer.Discovery
{
    /// <summary>
    /// Session 3: Reads live game data via reflection to verify we can access
    /// Customer and Dealer properties at runtime through IL2CPP interop.
    ///
    /// Must run AFTER a save game is loaded (triggered via OnSceneWasLoaded).
    /// READ-ONLY — never mutates game state.
    /// </summary>
    public static class RuntimeVerificationService
    {
        private const int MaxCustomersToLog = 5;

        /// <summary>
        /// Attempts to read Customer.UnlockedCustomers and Dealer.AllPlayerDealers.
        /// Returns true if data was accessible (even if lists are empty).
        /// Returns false if types/lists are null (game data not loaded yet — caller should retry).
        /// </summary>
        public static bool Verify()
        {
            ModLogger.Info("=== Runtime Verification ===");

            var customerType = FindType("Il2CppScheduleOne.Economy.Customer");
            var dealerType = FindType("Il2CppScheduleOne.Economy.Dealer");

            if (customerType == null || dealerType == null)
            {
                ModLogger.Warning($"Types not found — Customer: {customerType != null}, Dealer: {dealerType != null}");
                ModLogger.Info("=== Runtime Verification: SKIPPED (types not loaded) ===");
                return false;
            }

            ModLogger.Info($"Customer type: {customerType.FullName} (in {customerType.Assembly.GetName().Name})");
            ModLogger.Info($"Dealer type: {dealerType.FullName} (in {dealerType.Assembly.GetName().Name})");

            // Check if Customer.UnlockedCustomers is accessible and non-null
            var unlockedList = ReadStaticProperty(customerType, "UnlockedCustomers");
            if (unlockedList == null)
            {
                ModLogger.Warning("Customer.UnlockedCustomers is null — game data not loaded yet.");
                ModLogger.Info("=== Runtime Verification: DEFERRED ===");
                return false;
            }

            // Check if any data is actually populated (lists exist but may be empty on Menu scene)
            var dealerList = ReadStaticProperty(dealerType, "AllPlayerDealers");
            int customerCount = GetListCount(unlockedList);
            int dealerCount = dealerList != null ? GetListCount(dealerList) : 0;

            if (customerCount <= 0 && dealerCount <= 0)
            {
                ModLogger.Info($"Lists accessible but empty (customers: {customerCount}, dealers: {dealerCount}) — likely Menu scene, will retry.");
                ModLogger.Info("=== Runtime Verification: DEFERRED (no data yet) ===");
                return false;
            }

            // Data is populated — run full verification
            VerifyCustomers(customerType, unlockedList);
            VerifyDealers(dealerType);

            ModLogger.Info("=== Runtime Verification: COMPLETE ===");
            return true;
        }

        private static void VerifyCustomers(Type customerType, object unlockedList)
        {
            ModLogger.Info("--- Customers ---");

            int count = GetListCount(unlockedList);
            ModLogger.Info($"Customer.UnlockedCustomers: {count} customers (list type: {unlockedList.GetType().FullName})");

            if (count <= 0)
            {
                ModLogger.Info("No unlocked customers — new save or data not populated yet.");
                return;
            }

            int toRead = Math.Min(count, MaxCustomersToLog);
            ModLogger.Info($"Reading first {toRead} customers:");

            for (int i = 0; i < toRead; i++)
            {
                ModLogger.Info($"  --- Customer [{i}] ---");
                var customer = GetListItem(unlockedList, i);
                if (customer == null)
                {
                    ModLogger.Warning($"    [null object]");
                    continue;
                }

                ModLogger.Info($"    Runtime type: {customer.GetType().FullName}");
                LogCustomerDetails(customer);
            }
        }

        private static void LogCustomerDetails(object customer)
        {
            // --- Identity ---
            // Customer has-a NPC. Try name on NPC reference first, then directly.
            var npc = TryGetProperty(customer, "_NPC_k__BackingField")
                   ?? TryGetProperty(customer, "NPC");

            if (npc != null)
            {
                ModLogger.Info($"    NPC ref type: {npc.GetType().FullName}");
                TryLogProperty(npc, "fullName", "NPC.fullName");
                TryLogProperty(npc, "FirstName", "NPC.FirstName");
                TryLogProperty(npc, "LastName", "NPC.LastName");
                TryLogProperty(npc, "name", "NPC.name");
                TryLogProperty(npc, "Name", "NPC.Name");
                TryLogProperty(npc, "ID", "NPC.ID");
            }
            else
            {
                ModLogger.Debug("    No NPC reference found — trying name fields directly on Customer");
            }

            // Also try directly on Customer (might inherit or expose differently)
            TryLogProperty(customer, "fullName", "fullName");
            TryLogProperty(customer, "name", "name (UnityEngine.Object)");
            TryLogProperty(customer, "Name", "Name");

            // --- Assignment ---
            var dealer = TryGetProperty(customer, "_AssignedDealer_k__BackingField")
                      ?? TryGetProperty(customer, "AssignedDealer");

            if (dealer != null)
            {
                ModLogger.Info($"    AssignedDealer: [object] type={dealer.GetType().Name}");
                TryLogProperty(dealer, "fullName", "Dealer.fullName");
                TryLogProperty(dealer, "name", "Dealer.name");
                TryLogProperty(dealer, "Name", "Dealer.Name");
            }
            else
            {
                ModLogger.Info("    AssignedDealer: NULL — player-assigned?");
            }

            // --- Addiction ---
            TryLogProperty(customer, "_CurrentAddiction_k__BackingField", "CurrentAddiction (backing)");
            TryLogProperty(customer, "CurrentAddiction", "CurrentAddiction (prop)");

            // --- Spend (from CustomerData ScriptableObject) ---
            var custData = TryGetProperty(customer, "customerData")
                        ?? TryGetProperty(customer, "CustomerData");

            if (custData != null)
            {
                ModLogger.Info($"    CustomerData type: {custData.GetType().Name}");
                TryLogProperty(custData, "MinWeeklySpend", "MinWeeklySpend");
                TryLogProperty(custData, "MaxWeeklySpend", "MaxWeeklySpend");
                TryLogProperty(custData, "Standards", "Standards");
                TryLogProperty(custData, "name", "CustomerData.name");
            }
            else
            {
                ModLogger.Warning("    CustomerData: not accessible");
            }
        }

        private static void VerifyDealers(Type dealerType)
        {
            ModLogger.Info("--- Dealers ---");

            // MAX_CUSTOMERS (static)
            TryLogStaticProperty(dealerType, "MAX_CUSTOMERS", "Dealer.MAX_CUSTOMERS");

            var dealerList = ReadStaticProperty(dealerType, "AllPlayerDealers");
            if (dealerList == null)
            {
                ModLogger.Warning("Dealer.AllPlayerDealers is null");
                return;
            }

            int count = GetListCount(dealerList);
            ModLogger.Info($"Dealer.AllPlayerDealers: {count} dealers (list type: {dealerList.GetType().FullName})");

            if (count <= 0)
            {
                ModLogger.Info("No player dealers — none recruited yet in this save?");
                return;
            }

            for (int i = 0; i < count; i++)
            {
                ModLogger.Info($"  --- Dealer [{i}] ---");
                var dealer = GetListItem(dealerList, i);
                if (dealer == null)
                {
                    ModLogger.Warning($"    [null object]");
                    continue;
                }

                ModLogger.Info($"    Runtime type: {dealer.GetType().FullName}");
                LogDealerDetails(dealer);
            }
        }

        private static void LogDealerDetails(object dealer)
        {
            // --- Identity (Dealer IS-A NPC, so name fields are inherited) ---
            TryLogProperty(dealer, "fullName", "fullName");

            // --- Status ---
            TryLogProperty(dealer, "_IsRecruited_k__BackingField", "IsRecruited (backing)");
            TryLogProperty(dealer, "IsRecruited", "IsRecruited (prop)");

            // --- Assigned customers ---
            var assignedList = TryGetProperty(dealer, "_AssignedCustomers_k__BackingField")
                            ?? TryGetProperty(dealer, "AssignedCustomers");

            if (assignedList != null)
            {
                int custCount = GetListCount(assignedList);
                ModLogger.Info($"    AssignedCustomers: {custCount}");
            }
            else
            {
                ModLogger.Warning("    AssignedCustomers: not accessible");
            }

            // --- Stats ---
            TryLogProperty(dealer, "_Cash_k__BackingField", "Cash (backing)");
            TryLogProperty(dealer, "Cash", "Cash (prop)");
            TryLogProperty(dealer, "Cut", "Cut");
            TryLogProperty(dealer, "DealerType", "DealerType");
        }

        // ========== Helpers ==========

        private static object ReadStaticProperty(Type type, string propName)
        {
            try
            {
                var prop = type.GetProperty(propName,
                    BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);
                if (prop != null)
                    return prop.GetValue(null);

                var field = type.GetField(propName,
                    BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);
                if (field != null)
                    return field.GetValue(null);

                ModLogger.Debug($"Static member '{propName}' not found on {type.Name}");
            }
            catch (Exception ex)
            {
                ModLogger.Warning($"Error reading {type.Name}.{propName}: {ex.Message}");
            }
            return null;
        }

        private static void TryLogStaticProperty(Type type, string propName, string label)
        {
            var val = ReadStaticProperty(type, propName);
            if (val != null)
                ModLogger.Info($"  {label}: {val}");
            else
                ModLogger.Debug($"  {label}: not found or null");
        }

        private static object TryGetProperty(object obj, string propName)
        {
            try
            {
                var prop = obj.GetType().GetProperty(propName,
                    BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy);
                if (prop != null)
                    return prop.GetValue(obj);

                var field = obj.GetType().GetField(propName,
                    BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy);
                if (field != null)
                    return field.GetValue(obj);
            }
            catch (Exception ex)
            {
                ModLogger.Debug($"Error reading {propName}: {ex.Message}");
            }
            return null;
        }

        private static void TryLogProperty(object obj, string propName, string label)
        {
            try
            {
                var prop = obj.GetType().GetProperty(propName,
                    BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy);
                if (prop != null)
                {
                    var val = prop.GetValue(obj);
                    ModLogger.Info($"    {label}: {val ?? "null"}");
                    return;
                }

                var field = obj.GetType().GetField(propName,
                    BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy);
                if (field != null)
                {
                    var val = field.GetValue(obj);
                    ModLogger.Info($"    {label}: {val ?? "null"}");
                    return;
                }

                // Not found — only log at debug to reduce noise
                ModLogger.Debug($"    {label}: '{propName}' not found");
            }
            catch (Exception ex)
            {
                ModLogger.Warning($"    {label}: error — {ex.Message}");
            }
        }

        private static int GetListCount(object list)
        {
            try
            {
                var countProp = list.GetType().GetProperty("Count");
                if (countProp != null)
                    return (int)countProp.GetValue(list);

                var countMethod = list.GetType().GetMethod("get_Count");
                if (countMethod != null)
                    return (int)countMethod.Invoke(list, null);

                ModLogger.Warning($"No Count property on {list.GetType().FullName}");
            }
            catch (Exception ex)
            {
                ModLogger.Warning($"Error getting list count: {ex.Message}");
            }
            return -1;
        }

        private static object GetListItem(object list, int index)
        {
            try
            {
                // Try Item indexer property
                var indexer = list.GetType().GetProperty("Item");
                if (indexer != null)
                    return indexer.GetValue(list, new object[] { index });

                // Try get_Item method
                var getItem = list.GetType().GetMethod("get_Item");
                if (getItem != null)
                    return getItem.Invoke(list, new object[] { index });

                ModLogger.Warning($"No indexer on {list.GetType().FullName}");
            }
            catch (Exception ex)
            {
                ModLogger.Warning($"Error getting item [{index}]: {ex.Message}");
            }
            return null;
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
