using System;
using System.Reflection;
using ClientAssignmentOptimizer.Core;

namespace ClientAssignmentOptimizer.Services
{
    /// <summary>
    /// Performs customer reassignment via the game's public API methods:
    ///   Dealer.AddCustomer(Customer), Dealer.RemoveCustomer(Customer), Customer.AssignDealer(Dealer).
    ///
    /// Lookups are by NPC.ID / Dealer.fullName (stable identifiers) — never stored refs,
    /// so previously-fetched CustomerInfo/DealerInfo can't go stale.
    ///
    /// This is the first service in the project that MUTATES game state. Every call path
    /// logs before/after values so we can verify which sequence of calls the game accepts.
    /// </summary>
    public static class ReassignmentService
    {
        private static Type _customerType;
        private static Type _dealerType;
        private static PropertyInfo _unlockedCustomersProp;
        private static PropertyInfo _allDealersProp;
        private static MethodInfo _dealerAddCustomer;
        private static MethodInfo _dealerRemoveCustomer;
        private static MethodInfo _customerAssignDealer;
        private static PropertyInfo _maxCustomersProp;
        private static bool _resolved;

        public static bool IsReady => Resolve();

        /// <summary>
        /// Move a customer to player ownership (AssignedDealer = null).
        /// </summary>
        public static bool MoveToPlayer(string customerNpcId)
        {
            if (string.IsNullOrEmpty(customerNpcId)) return false;
            if (!Resolve()) { ModLogger.Warning("[Reassign] Types/methods not resolved."); return false; }

            var customer = FindCustomerById(customerNpcId);
            if (customer == null)
            {
                ModLogger.Warning($"[Reassign] Customer '{customerNpcId}' not found.");
                return false;
            }

            var customerName = GetCustomerDisplayName(customer);
            var oldDealer = GetCustomerDealer(customer);
            var oldDealerName = oldDealer != null ? GetDealerFullName(oldDealer) : "PLAYER";

            ModLogger.Info($"[Reassign] {customerName} ({customerNpcId}): {oldDealerName} -> PLAYER — start");

            if (oldDealer == null)
            {
                ModLogger.Info($"[Reassign] {customerName} is already player-assigned. No-op.");
                return true;
            }

            bool ok = true;
            ok &= InvokeRemoveCustomer(oldDealer, customer);
            ok &= InvokeAssignDealer(customer, null);

            LogPostState(customer, customerName, customerNpcId, oldDealer, null);
            return ok;
        }

        /// <summary>
        /// Move a customer to a specific dealer (looked up by fullName).
        /// </summary>
        public static bool MoveToDealer(string customerNpcId, string targetDealerFullName)
        {
            if (string.IsNullOrEmpty(customerNpcId) || string.IsNullOrEmpty(targetDealerFullName)) return false;
            if (!Resolve()) { ModLogger.Warning("[Reassign] Types/methods not resolved."); return false; }

            var customer = FindCustomerById(customerNpcId);
            if (customer == null) { ModLogger.Warning($"[Reassign] Customer '{customerNpcId}' not found."); return false; }

            var newDealer = FindDealerByName(targetDealerFullName);
            if (newDealer == null) { ModLogger.Warning($"[Reassign] Target dealer '{targetDealerFullName}' not found."); return false; }

            var customerName = GetCustomerDisplayName(customer);
            var oldDealer = GetCustomerDealer(customer);
            var oldDealerName = oldDealer != null ? GetDealerFullName(oldDealer) : "PLAYER";

            if (oldDealer != null && ReferenceEquals(oldDealer, newDealer))
            {
                ModLogger.Info($"[Reassign] {customerName} is already assigned to {targetDealerFullName}. No-op.");
                return true;
            }

            // Capacity check
            int newDealerCount = GetDealerCustomerCount(newDealer);
            int maxCustomers = ReadMaxCustomers();
            if (newDealerCount >= maxCustomers)
            {
                ModLogger.Warning($"[Reassign] {targetDealerFullName} is full ({newDealerCount}/{maxCustomers}). Aborting.");
                return false;
            }

            ModLogger.Info($"[Reassign] {customerName} ({customerNpcId}): {oldDealerName} -> {targetDealerFullName} (dealer has {newDealerCount}/{maxCustomers}) — start");

            bool ok = true;
            if (oldDealer != null) ok &= InvokeRemoveCustomer(oldDealer, customer);
            ok &= InvokeAddCustomer(newDealer, customer);
            ok &= InvokeAssignDealer(customer, newDealer);

            LogPostState(customer, customerName, customerNpcId, oldDealer, newDealer);
            return ok;
        }

        // ========== Method/type resolution ==========

        private static bool Resolve()
        {
            if (_resolved) return _customerType != null && _dealerType != null
                                 && _dealerAddCustomer != null && _dealerRemoveCustomer != null
                                 && _customerAssignDealer != null;

            _customerType = FindType("Il2CppScheduleOne.Economy.Customer");
            _dealerType = FindType("Il2CppScheduleOne.Economy.Dealer");

            if (_customerType != null)
            {
                _unlockedCustomersProp = _customerType.GetProperty("UnlockedCustomers",
                    BindingFlags.Public | BindingFlags.Static);

                // AssignDealer(Dealer) — single-arg
                foreach (var m in _customerType.GetMethods(BindingFlags.Public | BindingFlags.Instance))
                {
                    if (m.Name == "AssignDealer" && m.GetParameters().Length == 1)
                    {
                        _customerAssignDealer = m;
                        break;
                    }
                }
            }

            if (_dealerType != null)
            {
                _allDealersProp = _dealerType.GetProperty("AllPlayerDealers",
                    BindingFlags.Public | BindingFlags.Static);
                _maxCustomersProp = _dealerType.GetProperty("MAX_CUSTOMERS",
                    BindingFlags.Public | BindingFlags.Static);

                // Prefer the (Customer) overloads, not (string npcID)
                foreach (var m in _dealerType.GetMethods(BindingFlags.Public | BindingFlags.Instance))
                {
                    var ps = m.GetParameters();
                    if (ps.Length != 1) continue;
                    if (ps[0].ParameterType != _customerType) continue;

                    if (m.Name == "AddCustomer") _dealerAddCustomer = m;
                    else if (m.Name == "RemoveCustomer") _dealerRemoveCustomer = m;
                }
            }

            _resolved = true;

            if (_dealerAddCustomer == null) ModLogger.Warning("[Reassign] Dealer.AddCustomer(Customer) not found.");
            if (_dealerRemoveCustomer == null) ModLogger.Warning("[Reassign] Dealer.RemoveCustomer(Customer) not found.");
            if (_customerAssignDealer == null) ModLogger.Warning("[Reassign] Customer.AssignDealer(Dealer) not found.");

            return IsReady;
        }

        // ========== Game object lookup ==========

        private static object FindCustomerById(string npcId)
        {
            try
            {
                var list = _unlockedCustomersProp?.GetValue(null);
                if (list == null) return null;

                int count = GetListCount(list);
                for (int i = 0; i < count; i++)
                {
                    var customer = GetListItem(list, i);
                    if (customer == null) continue;

                    var npc = GetProp(customer, "_NPC_k__BackingField") ?? GetProp(customer, "NPC");
                    var id = npc != null ? GetPropString(npc, "ID") : null;
                    if (string.Equals(id, npcId, StringComparison.Ordinal)) return customer;
                }
            }
            catch (Exception ex)
            {
                ModLogger.Warning($"[Reassign] FindCustomerById failed: {ex.Message}");
            }
            return null;
        }

        private static object FindDealerByName(string fullName)
        {
            try
            {
                var list = _allDealersProp?.GetValue(null);
                if (list == null) return null;

                int count = GetListCount(list);
                for (int i = 0; i < count; i++)
                {
                    var dealer = GetListItem(list, i);
                    if (dealer == null) continue;

                    var dn = GetPropString(dealer, "fullName");
                    if (string.Equals(dn, fullName, StringComparison.Ordinal)) return dealer;
                }
            }
            catch (Exception ex)
            {
                ModLogger.Warning($"[Reassign] FindDealerByName failed: {ex.Message}");
            }
            return null;
        }

        private static int ReadMaxCustomers()
        {
            try
            {
                var v = _maxCustomersProp?.GetValue(null);
                if (v is int i) return i;
            }
            catch { }
            return 10; // known from Session 3
        }

        // ========== Method invocations ==========

        private static bool InvokeRemoveCustomer(object dealer, object customer)
        {
            try
            {
                _dealerRemoveCustomer.Invoke(dealer, new[] { customer });
                ModLogger.Info($"[Reassign]   RemoveCustomer on {GetDealerFullName(dealer)} -> ok");
                return true;
            }
            catch (Exception ex)
            {
                ModLogger.Warning($"[Reassign]   RemoveCustomer failed: {ex.InnerException?.Message ?? ex.Message}");
                return false;
            }
        }

        private static bool InvokeAddCustomer(object dealer, object customer)
        {
            try
            {
                _dealerAddCustomer.Invoke(dealer, new[] { customer });
                ModLogger.Info($"[Reassign]   AddCustomer on {GetDealerFullName(dealer)} -> ok");
                return true;
            }
            catch (Exception ex)
            {
                ModLogger.Warning($"[Reassign]   AddCustomer failed: {ex.InnerException?.Message ?? ex.Message}");
                return false;
            }
        }

        private static bool InvokeAssignDealer(object customer, object dealerOrNull)
        {
            try
            {
                _customerAssignDealer.Invoke(customer, new[] { dealerOrNull });
                var target = dealerOrNull != null ? GetDealerFullName(dealerOrNull) : "null (player)";
                ModLogger.Info($"[Reassign]   AssignDealer({target}) -> ok");
                return true;
            }
            catch (Exception ex)
            {
                ModLogger.Warning($"[Reassign]   AssignDealer failed: {ex.InnerException?.Message ?? ex.Message}");
                return false;
            }
        }

        // ========== Post-state logging ==========

        private static void LogPostState(object customer, string customerName, string customerId,
                                         object oldDealer, object newDealer)
        {
            try
            {
                var actualDealer = GetCustomerDealer(customer);
                var actualDealerName = actualDealer != null ? GetDealerFullName(actualDealer) : "PLAYER";

                int oldCount = oldDealer != null ? GetDealerCustomerCount(oldDealer) : -1;
                int newCount = newDealer != null ? GetDealerCustomerCount(newDealer) : -1;

                ModLogger.Info($"[Reassign] Post-state for {customerName}: AssignedDealer={actualDealerName}");
                if (oldDealer != null)
                    ModLogger.Info($"[Reassign]   Old dealer ({GetDealerFullName(oldDealer)}): {oldCount} customers");
                if (newDealer != null)
                    ModLogger.Info($"[Reassign]   New dealer ({GetDealerFullName(newDealer)}): {newCount} customers");
            }
            catch (Exception ex)
            {
                ModLogger.Warning($"[Reassign] LogPostState failed: {ex.Message}");
            }
        }

        // ========== Small helpers ==========

        private static string GetCustomerDisplayName(object customer)
        {
            var npc = GetProp(customer, "_NPC_k__BackingField") ?? GetProp(customer, "NPC");
            return npc != null ? GetPropString(npc, "fullName") ?? "Unknown" : "Unknown";
        }

        private static object GetCustomerDealer(object customer)
            => GetProp(customer, "AssignedDealer") ?? GetProp(customer, "_AssignedDealer_k__BackingField");

        private static string GetDealerFullName(object dealer)
            => GetPropString(dealer, "fullName") ?? "Unknown";

        private static int GetDealerCustomerCount(object dealer)
        {
            var list = GetProp(dealer, "AssignedCustomers") ?? GetProp(dealer, "_AssignedCustomers_k__BackingField");
            return list != null ? GetListCount(list) : 0;
        }

        // ========== Reflection primitives (mirrored from GameDataService) ==========

        private static object GetProp(object obj, string name)
        {
            try
            {
                var prop = obj.GetType().GetProperty(name,
                    BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy);
                if (prop != null) return prop.GetValue(obj);

                var field = obj.GetType().GetField(name,
                    BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy);
                if (field != null) return field.GetValue(obj);
            }
            catch { }
            return null;
        }

        private static string GetPropString(object obj, string name)
            => GetProp(obj, name)?.ToString();

        private static int GetListCount(object list)
        {
            try
            {
                var prop = list.GetType().GetProperty("Count");
                if (prop != null) return (int)prop.GetValue(list);
            }
            catch { }
            return 0;
        }

        private static object GetListItem(object list, int index)
        {
            try
            {
                var indexer = list.GetType().GetProperty("Item");
                if (indexer != null) return indexer.GetValue(list, new object[] { index });

                var method = list.GetType().GetMethod("get_Item");
                if (method != null) return method.Invoke(list, new object[] { index });
            }
            catch { }
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
