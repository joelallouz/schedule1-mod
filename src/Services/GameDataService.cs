using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using ClientAssignmentOptimizer.Core;
using ClientAssignmentOptimizer.Domain;

namespace ClientAssignmentOptimizer.Services
{
    /// <summary>
    /// Wraps IL2CPP reflection access into clean domain objects.
    /// Caches type/property lookups and query results for performance.
    /// READ-ONLY — never mutates game state.
    /// </summary>
    public static class GameDataService
    {
        // --- Type cache ---
        private static bool _typesResolved;
        private static Type _customerType;
        private static Type _dealerType;

        // --- Property cache (resolved lazily) ---
        private static PropertyInfo _unlockedCustomersProp;
        private static PropertyInfo _allDealersProp;

        // --- Data cache ---
        private static List<CustomerInfo> _cachedCustomers;
        private static List<DealerInfo> _cachedDealers;
        private static readonly Stopwatch _cacheTimer = new Stopwatch();
        private const double CacheLifetimeSeconds = 5.0;

        public static bool IsAvailable => ResolveTypes();

        public static List<CustomerInfo> GetAllCustomers()
        {
            if (_cachedCustomers != null && _cacheTimer.IsRunning
                && _cacheTimer.Elapsed.TotalSeconds < CacheLifetimeSeconds)
                return _cachedCustomers;

            _cachedCustomers = FetchCustomers();
            _cacheTimer.Restart();
            return _cachedCustomers;
        }

        public static List<DealerInfo> GetAllDealers()
        {
            if (_cachedDealers != null && _cacheTimer.IsRunning
                && _cacheTimer.Elapsed.TotalSeconds < CacheLifetimeSeconds)
                return _cachedDealers;

            _cachedDealers = FetchDealers();
            if (!_cacheTimer.IsRunning) _cacheTimer.Start();
            return _cachedDealers;
        }

        public static void InvalidateCache()
        {
            _cachedCustomers = null;
            _cachedDealers = null;
            _cacheTimer.Reset();
        }

        // ========== Fetchers ==========

        private static List<CustomerInfo> FetchCustomers()
        {
            var result = new List<CustomerInfo>();
            if (!ResolveTypes()) return result;

            try
            {
                var list = _unlockedCustomersProp.GetValue(null);
                if (list == null) return result;

                int count = GetListCount(list);
                for (int i = 0; i < count; i++)
                {
                    var customer = GetListItem(list, i);
                    if (customer == null) continue;

                    var info = MapCustomer(customer);
                    if (info != null) result.Add(info);
                }
            }
            catch (Exception ex)
            {
                ModLogger.Warning($"GameDataService.FetchCustomers failed: {ex.Message}");
            }

            return result;
        }

        private static List<DealerInfo> FetchDealers()
        {
            var result = new List<DealerInfo>();
            if (!ResolveTypes()) return result;

            try
            {
                var list = _allDealersProp.GetValue(null);
                if (list == null) return result;

                int count = GetListCount(list);
                for (int i = 0; i < count; i++)
                {
                    var dealer = GetListItem(list, i);
                    if (dealer == null) continue;

                    var info = MapDealer(dealer);
                    if (info != null) result.Add(info);
                }
            }
            catch (Exception ex)
            {
                ModLogger.Warning($"GameDataService.FetchDealers failed: {ex.Message}");
            }

            return result;
        }

        // ========== Mappers ==========

        private static CustomerInfo MapCustomer(object customer)
        {
            try
            {
                var npc = GetProp(customer, "_NPC_k__BackingField");
                var dealer = GetProp(customer, "AssignedDealer");
                var custData = GetProp(customer, "customerData");

                string dealerName = null;
                if (dealer != null)
                {
                    dealerName = GetPropString(dealer, "fullName") ?? "Unknown Dealer";
                }

                return new CustomerInfo
                {
                    FullName = npc != null ? GetPropString(npc, "fullName") ?? "Unknown" : "Unknown",
                    NpcId = npc != null ? GetPropString(npc, "ID") ?? "" : "",
                    AssignedDealerName = dealerName,
                    CurrentAddiction = GetPropFloat(customer, "CurrentAddiction"),
                    MinWeeklySpend = custData != null ? GetPropFloat(custData, "MinWeeklySpend") : 0f,
                    MaxWeeklySpend = custData != null ? GetPropFloat(custData, "MaxWeeklySpend") : 0f,
                    Standards = custData != null ? GetPropString(custData, "Standards") ?? "?" : "?",
                    Preferences = custData != null ? ReadPreferences(custData) : "",
                };
            }
            catch (Exception ex)
            {
                ModLogger.Debug($"MapCustomer failed: {ex.Message}");
                return null;
            }
        }

        private static DealerInfo MapDealer(object dealer)
        {
            try
            {
                var assignedList = GetProp(dealer, "AssignedCustomers")
                                ?? GetProp(dealer, "_AssignedCustomers_k__BackingField");
                int custCount = assignedList != null ? GetListCount(assignedList) : 0;

                return new DealerInfo
                {
                    FullName = GetPropString(dealer, "fullName") ?? "Unknown",
                    IsRecruited = GetPropBool(dealer, "IsRecruited"),
                    AssignedCustomerCount = custCount,
                    Cash = GetPropFloat(dealer, "Cash"),
                    Cut = GetPropFloat(dealer, "Cut"),
                    DealerType = GetPropString(dealer, "DealerType") ?? "?",
                };
            }
            catch (Exception ex)
            {
                ModLogger.Debug($"MapDealer failed: {ex.Message}");
                return null;
            }
        }

        private static bool _prefLoggedOnce = false;

        private static string ReadPreferences(object custData)
        {
            try
            {
                var prefList = GetProp(custData, "PreferredProperties");
                if (prefList == null) return "";

                int count = GetListCount(prefList);
                if (count <= 0) return "";

                var names = new List<string>();
                for (int i = 0; i < count; i++)
                {
                    var item = GetListItem(prefList, i);
                    if (item == null) continue;

                    // Log type info once for debugging
                    if (!_prefLoggedOnce)
                    {
                        ModLogger.Info($"[PrefDebug] Item type: {item.GetType().FullName}");
                        var props = item.GetType().GetProperties(
                            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                        foreach (var p in props.Take(20))
                            ModLogger.Info($"[PrefDebug]   {p.PropertyType.Name} {p.Name}");
                        _prefLoggedOnce = true;
                    }

                    // Try common property names for a readable value
                    var name = GetPropString(item, "Name")
                            ?? GetPropString(item, "name")
                            ?? GetPropString(item, "PropertyName")
                            ?? GetPropString(item, "Property")
                            ?? GetPropString(item, "DisplayName");

                    names.Add(name ?? item.ToString());
                }

                return string.Join(", ", names);
            }
            catch
            {
                return "";
            }
        }

        // ========== Type Resolution ==========

        private static bool ResolveTypes()
        {
            if (_typesResolved) return _customerType != null && _dealerType != null;

            _customerType = FindType("Il2CppScheduleOne.Economy.Customer");
            _dealerType = FindType("Il2CppScheduleOne.Economy.Dealer");

            if (_customerType != null)
            {
                _unlockedCustomersProp = _customerType.GetProperty("UnlockedCustomers",
                    BindingFlags.Public | BindingFlags.Static);
            }

            if (_dealerType != null)
            {
                _allDealersProp = _dealerType.GetProperty("AllPlayerDealers",
                    BindingFlags.Public | BindingFlags.Static);
            }

            _typesResolved = true;
            return _customerType != null && _dealerType != null;
        }

        // ========== Reflection Helpers ==========

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
        {
            var val = GetProp(obj, name);
            return val?.ToString();
        }

        private static float GetPropFloat(object obj, string name)
        {
            var val = GetProp(obj, name);
            if (val is float f) return f;
            if (val != null && float.TryParse(val.ToString(), out float parsed)) return parsed;
            return 0f;
        }

        private static bool GetPropBool(object obj, string name)
        {
            var val = GetProp(obj, name);
            if (val is bool b) return b;
            return false;
        }

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
