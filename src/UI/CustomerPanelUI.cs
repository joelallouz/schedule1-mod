using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using ClientAssignmentOptimizer.Core;
using ClientAssignmentOptimizer.Domain;
using ClientAssignmentOptimizer.Services;

namespace ClientAssignmentOptimizer.UI
{
    /// <summary>
    /// IMGUI panel showing all customers and their stats.
    /// Toggle with F9. Click a row to select; action bar above the table reassigns the selected customer.
    /// </summary>
    public static class CustomerPanelUI
    {
        private static bool _visible;
        private static Vector2 _scrollPos;
        private static List<CustomerInfo> _displayCustomers = new List<CustomerInfo>();
        private static List<DealerInfo> _displayDealers = new List<DealerInfo>();
        private static SortColumn _sortColumn = SortColumn.MaxSpend;
        private static bool _sortAscending = false;
        private static string _selectedNpcId;  // stable across refreshes

        private static readonly Color PlayerColor = new Color(0.4f, 1f, 0.4f);
        private static readonly Color DealerColor = new Color(1f, 1f, 1f);
        private static readonly Color HeaderColor = new Color(0.8f, 0.8f, 0.8f);
        private static readonly Color SelectedColor = new Color(1f, 0.85f, 0.4f);

        private const float ColSelect = 18f;
        private const float ColName = 150f;
        private const float ColAssign = 120f;
        private const float ColAddiction = 70f;
        private const float ColMinSpend = 70f;
        private const float ColMaxSpend = 70f;
        private const float ColStandards = 80f;
        private const float ColPrefs = 180f;

        private const float WindowWidth = 840f;
        private const float WindowHeight = 540f;
        private const float Margin = 10f;

        public static bool Visible => _visible;

        public static void Toggle()
        {
            _visible = !_visible;
            if (_visible) Refresh();
        }

        public static void Refresh()
        {
            GameDataService.InvalidateCache();
            _displayCustomers = GameDataService.GetAllCustomers();
            _displayDealers = GameDataService.GetAllDealers();
            ApplySort();
        }

        public static void Draw()
        {
            if (!_visible) return;

            float x = (Screen.width - WindowWidth) / 2f;
            float y = (Screen.height - WindowHeight) / 2f;
            var windowRect = new Rect(x, y, WindowWidth, WindowHeight);

            // Dark background
            GUI.Box(windowRect, "");
            GUI.Box(windowRect, "");

            GUILayout.BeginArea(new Rect(x + Margin, y + Margin,
                WindowWidth - Margin * 2, WindowHeight - Margin * 2));

            DrawHeader();
            DrawActionBar();
            DrawColumnHeaders();
            DrawCustomerList();

            GUILayout.EndArea();
        }

        private static void DrawActionBar()
        {
            var selected = GetSelectedCustomer();
            if (selected == null)
            {
                GUILayout.Label("<i>Click a customer row to select, then reassign.</i>");
                GUILayout.Space(4);
                return;
            }

            GUILayout.BeginHorizontal();

            var prev = GUI.contentColor;
            GUI.contentColor = SelectedColor;
            GUILayout.Label($"Selected: {selected.FullName}  ({(selected.IsPlayerAssigned ? "PLAYER" : selected.AssignedDealerName)})",
                GUILayout.Width(300));
            GUI.contentColor = prev;

            if (GUILayout.Button("Deselect", GUILayout.Width(80))) _selectedNpcId = null;

            GUILayout.Label("Move to:", GUILayout.Width(60));

            // Move-to-player button (only if not already player-assigned)
            if (!selected.IsPlayerAssigned)
            {
                if (GUILayout.Button("Player", GUILayout.Width(70)))
                {
                    ModLogger.Info($"User requested: move {selected.FullName} -> PLAYER");
                    if (ReassignmentService.MoveToPlayer(selected.NpcId))
                        Refresh();
                }
            }

            // One button per recruited dealer that isn't the current assignment
            foreach (var d in _displayDealers.Where(d => d.IsRecruited).OrderBy(d => d.FullName))
            {
                if (selected.AssignedDealerName == d.FullName) continue;

                bool full = d.AssignedCustomerCount >= 10;
                string label = full ? $"{d.FullName} (full)" : d.FullName;

                GUI.enabled = !full;
                if (GUILayout.Button(label, GUILayout.Width(130)))
                {
                    ModLogger.Info($"User requested: move {selected.FullName} -> {d.FullName}");
                    if (ReassignmentService.MoveToDealer(selected.NpcId, d.FullName))
                        Refresh();
                }
                GUI.enabled = true;
            }

            GUILayout.EndHorizontal();
            GUILayout.Space(4);
        }

        private static CustomerInfo GetSelectedCustomer()
        {
            if (string.IsNullOrEmpty(_selectedNpcId)) return null;
            return _displayCustomers.FirstOrDefault(c => c.NpcId == _selectedNpcId);
        }

        private static void DrawHeader()
        {
            var prevColor = GUI.contentColor;
            GUI.contentColor = HeaderColor;

            GUILayout.Label("<b>Client Assignment Optimizer</b> — F9 to close");

            int recruited = _displayDealers.Count(d => d.IsRecruited);
            int playerAssigned = _displayCustomers.Count(c => c.IsPlayerAssigned);
            int dealerAssigned = _displayCustomers.Count - playerAssigned;

            GUILayout.Label($"{_displayCustomers.Count} customers ({playerAssigned} player, {dealerAssigned} dealer) | {recruited} dealers recruited");

            GUI.contentColor = prevColor;

            GUILayout.Space(4);
        }

        private static void DrawColumnHeaders()
        {
            GUILayout.BeginHorizontal();

            GUILayout.Label("", GUILayout.Width(ColSelect));
            DrawSortButton("Name", SortColumn.Name, ColName);
            DrawSortButton("Assignment", SortColumn.Assignment, ColAssign);
            DrawSortButton("Addiction", SortColumn.Addiction, ColAddiction);
            DrawSortButton("Min $", SortColumn.MinSpend, ColMinSpend);
            DrawSortButton("Max $", SortColumn.MaxSpend, ColMaxSpend);
            DrawSortButton("Standards", SortColumn.Standards, ColStandards);
            DrawSortButton("Preferences", SortColumn.Preferences, ColPrefs);

            GUILayout.EndHorizontal();
        }

        private static void DrawSortButton(string label, SortColumn column, float width)
        {
            string display = label;
            if (_sortColumn == column)
                display += _sortAscending ? " ▲" : " ▼";

            if (GUILayout.Button(display, GUILayout.Width(width)))
            {
                if (_sortColumn == column)
                    _sortAscending = !_sortAscending;
                else
                {
                    _sortColumn = column;
                    _sortAscending = column == SortColumn.Name || column == SortColumn.Assignment;
                }
                ApplySort();
            }
        }

        private static void DrawCustomerList()
        {
            _scrollPos = GUILayout.BeginScrollView(_scrollPos);

            var prevColor = GUI.contentColor;

            foreach (var c in _displayCustomers)
            {
                bool isSelected = c.NpcId == _selectedNpcId;
                GUI.contentColor = isSelected
                    ? SelectedColor
                    : (c.IsPlayerAssigned ? PlayerColor : DealerColor);

                GUILayout.BeginHorizontal();

                // Per-row select button — native click handling, no GetLastRect (stripped in this IL2CPP build)
                if (GUILayout.Button(isSelected ? "►" : " ", GUILayout.Width(ColSelect)))
                {
                    _selectedNpcId = isSelected ? null : c.NpcId;
                }

                GUILayout.Label(c.FullName, GUILayout.Width(ColName));
                GUILayout.Label(c.IsPlayerAssigned ? "PLAYER" : c.AssignedDealerName, GUILayout.Width(ColAssign));
                GUILayout.Label(c.CurrentAddiction.ToString("F2"), GUILayout.Width(ColAddiction));
                GUILayout.Label($"${c.MinWeeklySpend:F0}", GUILayout.Width(ColMinSpend));
                GUILayout.Label($"${c.MaxWeeklySpend:F0}", GUILayout.Width(ColMaxSpend));
                GUILayout.Label(c.Standards, GUILayout.Width(ColStandards));
                GUILayout.Label(c.Preferences, GUILayout.Width(ColPrefs));
                GUILayout.EndHorizontal();
            }

            GUI.contentColor = prevColor;

            GUILayout.EndScrollView();
        }

        private static void ApplySort()
        {
            _displayCustomers = _sortColumn switch
            {
                SortColumn.Name => _sortAscending
                    ? _displayCustomers.OrderBy(c => c.FullName).ToList()
                    : _displayCustomers.OrderByDescending(c => c.FullName).ToList(),
                SortColumn.Assignment => _sortAscending
                    ? _displayCustomers.OrderBy(c => c.AssignedDealerName ?? "").ToList()
                    : _displayCustomers.OrderByDescending(c => c.AssignedDealerName ?? "").ToList(),
                SortColumn.Addiction => _sortAscending
                    ? _displayCustomers.OrderBy(c => c.CurrentAddiction).ToList()
                    : _displayCustomers.OrderByDescending(c => c.CurrentAddiction).ToList(),
                SortColumn.MinSpend => _sortAscending
                    ? _displayCustomers.OrderBy(c => c.MinWeeklySpend).ToList()
                    : _displayCustomers.OrderByDescending(c => c.MinWeeklySpend).ToList(),
                SortColumn.MaxSpend => _sortAscending
                    ? _displayCustomers.OrderBy(c => c.MaxWeeklySpend).ToList()
                    : _displayCustomers.OrderByDescending(c => c.MaxWeeklySpend).ToList(),
                SortColumn.Standards => _sortAscending
                    ? _displayCustomers.OrderBy(c => c.Standards).ToList()
                    : _displayCustomers.OrderByDescending(c => c.Standards).ToList(),
                SortColumn.Preferences => _sortAscending
                    ? _displayCustomers.OrderBy(c => c.Preferences).ToList()
                    : _displayCustomers.OrderByDescending(c => c.Preferences).ToList(),
                _ => _displayCustomers,
            };
        }

        private enum SortColumn
        {
            Name,
            Assignment,
            Addiction,
            MinSpend,
            MaxSpend,
            Standards,
            Preferences,
        }
    }
}
