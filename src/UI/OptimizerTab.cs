using System;
using System.Collections.Generic;
using System.Reflection;
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.InteropTypes;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using ClientAssignmentOptimizer.Core;
using ClientAssignmentOptimizer.Domain;
using ClientAssignmentOptimizer.Services;

namespace ClientAssignmentOptimizer.UI
{
    /// <summary>
    /// Injected into DealerManagementApp. The optimizer panel is parented inside the
    /// phone's appContainer (a child of the existing phone canvas), so it inherits
    /// the phone's screen rect and renders inside the phone like a real app.
    /// </summary>
    public static class OptimizerTab
    {
        // ---- Phone reflection (cached after first resolve) ----
        private static Type _phoneType;
        private static PropertyInfo _phoneInstanceProp;
        private static MethodInfo _phoneSetIsHorizontal;
        private static bool _phoneResolved;

        // ---- Owned UI ----
        private static object _currentDealerApp;
        private static RectTransform _appContainer;
        private static GameObject _vanillaContentGO;
        private static GameObject _toggleButtonGO;
        private static GameObject _optimizerPanelGO;
        private static GameObject _tableContentGO;
        private static GameObject _reassignPopupGO;

        private static bool _isOptimizerActive;
        private static Font _cachedFont;

        // ---- Sort state ----
        // -1 = no sort (insertion order). Otherwise an index into ColumnSortKeys.
        private static int _sortColumn = -1;
        private static bool _sortAscending = true;

        // 7 columns. ★ at index 0 (sortable: flagged-first, MaxWeeklySpend tie-break).
        // Reassign at index 6 is the only non-sortable column.
        private static readonly float[] ColumnWeights = { 0.3f, 1.7f, 1.3f, 0.8f, 1.4f, 2.0f, 1.4f };
        private static readonly string[] ColumnHeaders = { "★", "Name", "Assigned", "Addiction", "Weekly $", "Preferences", "Reassign" };
        private static readonly bool[] ColumnSortable = { true, true, true, true, true, true, false };

        // =====================================================================
        // Harmony-patch entry points
        // =====================================================================

        public static void OnDealerAppOpened(object dealerApp)
        {
            try
            {
                ModLogger.Info($"[OptimizerTab] OnDealerAppOpened: dealerApp type = {dealerApp?.GetType().FullName ?? "null"}");

                if (!ReferenceEquals(_currentDealerApp, dealerApp) || _toggleButtonGO == null)
                {
                    DisposeOwnedUI();
                    _currentDealerApp = dealerApp;
                    InjectToggleButton(dealerApp);
                }

                if (_isOptimizerActive)
                {
                    ExitOptimizerMode();
                }
            }
            catch (Exception ex)
            {
                ModLogger.Error($"[OptimizerTab] OnDealerAppOpened: {ex.GetType().Name}: {ex.Message}");
                ModLogger.Error($"[OptimizerTab] Stack: {ex.StackTrace}");
                if (ex.InnerException != null)
                    ModLogger.Error($"[OptimizerTab] Inner: {ex.InnerException.GetType().Name}: {ex.InnerException.Message}");
            }
        }

        public static void OnDealerAppClosed(object dealerApp)
        {
            try
            {
                if (_isOptimizerActive)
                {
                    if (_optimizerPanelGO != null) _optimizerPanelGO.SetActive(false);
                    ShowVanilla();
                    if (_toggleButtonGO != null) _toggleButtonGO.SetActive(true);
                    _isOptimizerActive = false;
                }
                CloseReassignPopup();
            }
            catch (Exception ex)
            {
                ModLogger.Error($"[OptimizerTab] OnDealerAppClosed: {ex.Message}");
            }
        }

        // =====================================================================
        // Toggle button injection
        // =====================================================================

        private static void InjectToggleButton(object dealerApp)
        {
            var containerT = ReflectGet<Transform>(dealerApp, "appContainer");
            if (containerT == null)
            {
                ModLogger.Warning("[OptimizerTab] appContainer not found on DealerApp — cannot inject.");
                return;
            }
            _appContainer = containerT.GetComponent<RectTransform>();
            if (_appContainer == null)
            {
                ModLogger.Warning("[OptimizerTab] appContainer has no RectTransform — cannot inject.");
                return;
            }

            var contentT = ReflectGet<Transform>(dealerApp, "Content");
            if (contentT != null) _vanillaContentGO = contentT.gameObject;

            _toggleButtonGO = CreateButton(_appContainer, "OptimizerToggle", "Optimize",
                () => ToggleOptimizerMode());

            var rt = _toggleButtonGO.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(1f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(1f, 1f);
            rt.anchoredPosition = new Vector2(-14f, -14f);
            rt.sizeDelta = new Vector2(160f, 48f);

            ModLogger.Info("[OptimizerTab] Optimize toggle button injected.");
        }

        public static void ToggleOptimizerMode()
        {
            if (_isOptimizerActive) ExitOptimizerMode();
            else EnterOptimizerMode();
        }

        private static void EnterOptimizerMode()
        {
            ModLogger.Info("[OptimizerTab] Entering optimizer mode.");

            if (_optimizerPanelGO == null) BuildOptimizerPanel();
            HideVanilla();
            _optimizerPanelGO.SetActive(true);
            _optimizerPanelGO.transform.SetAsLastSibling();

            // Hide the portrait-anchored toggle while in landscape optimizer mode —
            // the panel has its own Close at the bottom and the toggle would otherwise
            // read sideways inside the rotated appContainer.
            if (_toggleButtonGO != null) _toggleButtonGO.SetActive(false);

            SetPhoneHorizontal(true);
            ApplyLandscapePanelLayout(true);

            RefreshTable();

            _isOptimizerActive = true;
        }

        private static void ExitOptimizerMode()
        {
            ModLogger.Info("[OptimizerTab] Exiting optimizer mode.");

            CloseReassignPopup();
            if (_optimizerPanelGO != null) _optimizerPanelGO.SetActive(false);
            ApplyLandscapePanelLayout(false);
            SetPhoneHorizontal(false);
            ShowVanilla();

            if (_toggleButtonGO != null) _toggleButtonGO.SetActive(true);

            _isOptimizerActive = false;
        }

        // The phone rotates the entire phone model +90° (or -90°) in world space.
        // Without compensation, our panel — parented inside appContainer — rotates
        // with the phone and reads sideways. Counter-rotate the panel locally so the
        // two rotations cancel for text legibility, and feed it the appContainer's
        // height as its width and vice versa so it occupies the now-landscape rect.
        private static void ApplyLandscapePanelLayout(bool landscape)
        {
            if (_optimizerPanelGO == null || _appContainer == null) return;
            var rt = _optimizerPanelGO.GetComponent<RectTransform>();
            if (rt == null) return;

            if (landscape)
            {
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = Vector2.zero;
                rt.sizeDelta = new Vector2(_appContainer.rect.height, _appContainer.rect.width);
                rt.localRotation = Quaternion.Euler(0f, 0f, -90f);
            }
            else
            {
                rt.localRotation = Quaternion.identity;
                FillParent(rt);
            }
        }

        private static void HideVanilla()
        {
            if (_vanillaContentGO != null) _vanillaContentGO.SetActive(false);
        }

        private static void ShowVanilla()
        {
            if (_vanillaContentGO != null) _vanillaContentGO.SetActive(true);
        }

        // =====================================================================
        // Optimizer panel (built lazily on first enter)
        // =====================================================================

        private static void BuildOptimizerPanel()
        {
            // Panel fills the dealer-app's appContainer — inherits the phone canvas's
            // render mode, sort order, and screen rect for free.
            _optimizerPanelGO = NewUIGameObject("OptimizerPanel");
            _optimizerPanelGO.transform.SetParent(_appContainer, false);
            FillParent(_optimizerPanelGO.GetComponent<RectTransform>());
            // If appContainer has a LayoutGroup, opt out so our full-stretch anchors
            // are not overwritten by the layout pass.
            var panelLE = _optimizerPanelGO.AddComponent<LayoutElement>();
            panelLE.ignoreLayout = true;

            var bg = _optimizerPanelGO.AddComponent<Image>();
            bg.color = new Color(0.08f, 0.08f, 0.10f, 1f);
            bg.raycastTarget = true;

            // Title (top, narrow band, click-through so it never blocks header taps).
            var title = CreateText(_optimizerPanelGO.transform, "Title", "Customer Optimizer",
                36, TextAnchor.MiddleCenter);
            AnchorTopStretch(title.GetComponent<RectTransform>(), -8f, 44f);
            var titleTxt = title.GetComponent<Text>();
            if (titleTxt != null) titleTxt.raycastTarget = false;

            // Header row (stretches across the panel just below the title).
            var header = NewUIGameObject("Header");
            header.transform.SetParent(_optimizerPanelGO.transform, false);
            AnchorTopStretch(header.GetComponent<RectTransform>(), -64f, 44f);
            var headerBg = header.AddComponent<Image>();
            headerBg.color = new Color(0.18f, 0.18f, 0.22f, 1f);
            headerBg.raycastTarget = false;
            BuildHeaderCells(header.transform);

            // Scroll view: occupies the remaining vertical space below the header,
            // leaving a strip at the bottom for the in-panel Close button.
            var scrollGO = NewUIGameObject("ScrollView");
            scrollGO.transform.SetParent(_optimizerPanelGO.transform, false);
            var scrollRT = scrollGO.GetComponent<RectTransform>();
            scrollRT.anchorMin = new Vector2(0f, 0f);
            scrollRT.anchorMax = new Vector2(1f, 1f);
            scrollRT.offsetMin = new Vector2(8f, 68f);
            scrollRT.offsetMax = new Vector2(-8f, -116f);
            var scrollBg = scrollGO.AddComponent<Image>();
            scrollBg.color = new Color(0.12f, 0.12f, 0.14f, 1f);
            var scrollRect = scrollGO.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;

            var viewport = NewUIGameObject("Viewport");
            viewport.transform.SetParent(scrollGO.transform, false);
            var vpRT = viewport.GetComponent<RectTransform>();
            FillParent(vpRT);
            var vpImg = viewport.AddComponent<Image>();
            vpImg.color = new Color(0f, 0f, 0f, 0.01f);
            var mask = viewport.AddComponent<Mask>();
            mask.showMaskGraphic = false;

            _tableContentGO = NewUIGameObject("Content");
            _tableContentGO.transform.SetParent(viewport.transform, false);
            var contentRT = _tableContentGO.GetComponent<RectTransform>();
            contentRT.anchorMin = new Vector2(0f, 1f);
            contentRT.anchorMax = new Vector2(1f, 1f);
            contentRT.pivot = new Vector2(0.5f, 1f);
            contentRT.anchoredPosition = Vector2.zero;
            contentRT.sizeDelta = new Vector2(0f, 0f);
            var vlg = _tableContentGO.AddComponent<VerticalLayoutGroup>();
            vlg.childAlignment = TextAnchor.UpperLeft;
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.spacing = 2f;
            vlg.padding = new RectOffset(2, 2, 2, 2);
            var csf = _tableContentGO.AddComponent<ContentSizeFitter>();
            csf.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scrollRect.viewport = vpRT;
            scrollRect.content = contentRT;

            // In-panel Close button (bottom-right). Independent of the dealer-app toggle.
            var closeBtn = CreateButton(_optimizerPanelGO.transform, "CloseBtn", "Close",
                () => ExitOptimizerMode());
            SetButtonColor(closeBtn, new Color(0.5f, 0.2f, 0.2f, 1f));
            var closeRT = closeBtn.GetComponent<RectTransform>();
            closeRT.anchorMin = new Vector2(1f, 0f);
            closeRT.anchorMax = new Vector2(1f, 0f);
            closeRT.pivot = new Vector2(1f, 0f);
            closeRT.anchoredPosition = new Vector2(-12f, 12f);
            closeRT.sizeDelta = new Vector2(160f, 52f);

            _optimizerPanelGO.SetActive(false);
        }

        private static void BuildHeaderCells(Transform parent)
        {
            float total = 0f;
            foreach (var w in ColumnWeights) total += w;

            float acc = 0f;
            for (int i = 0; i < ColumnHeaders.Length; i++)
            {
                float w = ColumnWeights[i];
                float xMin = acc / total;
                float xMax = (acc + w) / total;
                acc += w;

                if (ColumnSortable[i])
                {
                    int columnIndex = i;
                    var cell = CreateButton(parent, $"H_{i}", HeaderLabel(i),
                        () => OnHeaderClick(columnIndex));
                    var rt = cell.GetComponent<RectTransform>();
                    rt.anchorMin = new Vector2(xMin, 0f);
                    rt.anchorMax = new Vector2(xMax, 1f);
                    rt.offsetMin = new Vector2(2f, 1f);
                    rt.offsetMax = new Vector2(-2f, -1f);
                    var img = cell.GetComponent<Image>();
                    if (img != null) img.color = new Color(0.22f, 0.22f, 0.28f, 1f);
                    var txt = cell.GetComponentInChildren<Text>();
                    if (txt != null)
                    {
                        txt.fontSize = 28;
                        txt.fontStyle = FontStyle.Bold;
                        txt.alignment = TextAnchor.MiddleLeft;
                    }
                }
                else
                {
                    var cell = NewUIGameObject($"H_{i}");
                    cell.transform.SetParent(parent, false);
                    var rt = cell.GetComponent<RectTransform>();
                    rt.anchorMin = new Vector2(xMin, 0f);
                    rt.anchorMax = new Vector2(xMax, 1f);
                    rt.offsetMin = new Vector2(4f, 2f);
                    rt.offsetMax = new Vector2(-4f, -2f);

                    var txt = cell.AddComponent<Text>();
                    txt.text = ColumnHeaders[i];
                    txt.font = GetFont();
                    txt.fontSize = 28;
                    txt.fontStyle = FontStyle.Bold;
                    txt.color = new Color(0.95f, 0.95f, 0.95f, 1f);
                    txt.alignment = TextAnchor.MiddleLeft;
                    txt.raycastTarget = false;
                }
            }
        }

        private static string HeaderLabel(int columnIndex)
        {
            string indicator = "";
            if (_sortColumn == columnIndex) indicator = _sortAscending ? "  ▲" : "  ▼";
            return ColumnHeaders[columnIndex] + indicator;
        }

        private static void OnHeaderClick(int columnIndex)
        {
            if (_sortColumn == columnIndex)
            {
                _sortAscending = !_sortAscending;
            }
            else
            {
                _sortColumn = columnIndex;
                _sortAscending = true;
            }
            ModLogger.Info($"[OptimizerTab] Sort: column={_sortColumn} ({ColumnHeaders[_sortColumn]}), ascending={_sortAscending}");
            RebuildHeaderLabels();
            RefreshTable();
        }

        private static void RebuildHeaderLabels()
        {
            // The header is rebuilt by simply rebuilding the panel headers in place.
            // Walk the children of the Header GO and update Text on the sortable cells.
            if (_optimizerPanelGO == null) return;
            var headerT = _optimizerPanelGO.transform.Find("Header");
            if (headerT == null) return;
            for (int i = 0; i < ColumnHeaders.Length; i++)
            {
                if (!ColumnSortable[i]) continue;
                var cellT = headerT.Find($"H_{i}");
                if (cellT == null) continue;
                var txt = cellT.GetComponentInChildren<Text>();
                if (txt != null) txt.text = HeaderLabel(i);
            }
        }

        // =====================================================================
        // Table refresh + rows
        // =====================================================================

        public static void RefreshTable()
        {
            if (_tableContentGO == null) return;

            ClearChildren(_tableContentGO.transform);

            GameDataService.InvalidateCache();
            var customers = GameDataService.GetAllCustomers();
            var dealers = GameDataService.GetAllDealers();

            var recruited = new List<DealerInfo>();
            foreach (var d in dealers)
            {
                if (d.IsRecruited) recruited.Add(d);
            }

            var sorted = SortCustomers(customers);

            int flaggedCount = 0;
            foreach (var c in sorted) if (c.ShouldBePlayer) flaggedCount++;
            ModLogger.Info($"[OptimizerTab] {flaggedCount} of {sorted.Count} customers flagged (threshold ${ModConfig.SpendThreshold}).");

            foreach (var c in sorted)
            {
                AddCustomerRow(c, recruited);
            }

            ModLogger.Info($"[OptimizerTab] Table refreshed: {sorted.Count} customers, {recruited.Count} recruited dealers (sortCol={_sortColumn}, asc={_sortAscending}).");
        }

        private static List<CustomerInfo> SortCustomers(List<CustomerInfo> input)
        {
            var copy = new List<CustomerInfo>(input);
            if (_sortColumn < 0) return copy;

            Comparison<CustomerInfo> cmp;
            switch (_sortColumn)
            {
                case 0: // ★ Flag — flagged first, MaxWeeklySpend desc tie-break within flagged
                    cmp = (a, b) =>
                    {
                        int byFlag = b.ShouldBePlayer.CompareTo(a.ShouldBePlayer);
                        if (byFlag != 0) return byFlag;
                        return b.MaxWeeklySpend.CompareTo(a.MaxWeeklySpend);
                    };
                    break;
                case 1: // Name
                    cmp = (a, b) => string.Compare(a.FullName ?? "", b.FullName ?? "", StringComparison.OrdinalIgnoreCase);
                    break;
                case 2: // Assigned
                    cmp = (a, b) => string.Compare(AssignedLabel(a), AssignedLabel(b), StringComparison.OrdinalIgnoreCase);
                    break;
                case 3: // Addiction
                    cmp = (a, b) => a.CurrentAddiction.CompareTo(b.CurrentAddiction);
                    break;
                case 4: // Weekly $ — sort by max spend
                    cmp = (a, b) => a.MaxWeeklySpend.CompareTo(b.MaxWeeklySpend);
                    break;
                case 5: // Preferences
                    cmp = (a, b) => string.Compare(a.Preferences ?? "", b.Preferences ?? "", StringComparison.OrdinalIgnoreCase);
                    break;
                default:
                    return copy;
            }

            copy.Sort(cmp);
            if (!_sortAscending) copy.Reverse();
            return copy;
        }

        private static string AssignedLabel(CustomerInfo c)
        {
            return c.IsPlayerAssigned ? "Player" : (c.AssignedDealerName ?? "?");
        }

        private static void AddCustomerRow(CustomerInfo c, List<DealerInfo> recruited)
        {
            bool flagOn = ModConfig.EnableFlagging && c.ShouldBePlayer;

            var row = NewUIGameObject($"Row_{c.NpcId}");
            row.transform.SetParent(_tableContentGO.transform, false);
            var rowImg = row.AddComponent<Image>();
            rowImg.color = flagOn
                ? new Color(0.30f, 0.22f, 0.10f, 0.9f)  // amber for flagged dealer customers
                : new Color(0.18f, 0.18f, 0.20f, 0.9f); // default
            rowImg.raycastTarget = false;
            var le = row.AddComponent<LayoutElement>();
            le.preferredHeight = 58f;
            le.minHeight = 58f;

            string assign = AssignedLabel(c);
            string spend = "$" + ((int)c.MinWeeklySpend) + " – $" + ((int)c.MaxWeeklySpend);
            string flagText = flagOn ? "★" : "";

            var cells = new[]
            {
                flagText,
                c.FullName ?? "",
                assign,
                c.CurrentAddiction.ToString("0.00"),
                spend,
                c.Preferences ?? "",
            };

            float total = 0f;
            foreach (var w in ColumnWeights) total += w;
            float acc = 0f;

            for (int i = 0; i < cells.Length; i++)
            {
                float w = ColumnWeights[i];
                float xMin = acc / total;
                float xMax = (acc + w) / total;
                acc += w;

                var cell = NewUIGameObject($"Cell_{i}");
                cell.transform.SetParent(row.transform, false);
                var rt = cell.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(xMin, 0f);
                rt.anchorMax = new Vector2(xMax, 1f);
                rt.offsetMin = new Vector2(4f, 1f);
                rt.offsetMax = new Vector2(-4f, -1f);

                var txt = cell.AddComponent<Text>();
                txt.text = cells[i];
                txt.font = GetFont();
                txt.fontSize = 26;
                if (i == 0) // ★ flag column — gold, centered, bold
                {
                    txt.color = new Color(1.0f, 0.85f, 0.3f, 1.0f);
                    txt.alignment = TextAnchor.MiddleCenter;
                    txt.fontStyle = FontStyle.Bold;
                }
                else
                {
                    txt.color = new Color(0.88f, 0.88f, 0.88f, 1f);
                    txt.alignment = TextAnchor.MiddleLeft;
                }
                txt.horizontalOverflow = HorizontalWrapMode.Overflow;
                txt.verticalOverflow = VerticalWrapMode.Truncate;
                txt.raycastTarget = false;
            }

            // Reassign cell (last column).
            {
                float w = ColumnWeights[cells.Length];
                float xMin = acc / total;
                float xMax = (acc + w) / total;

                var cell = NewUIGameObject("Cell_Reassign");
                cell.transform.SetParent(row.transform, false);
                var rt = cell.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(xMin, 0f);
                rt.anchorMax = new Vector2(xMax, 1f);
                rt.offsetMin = new Vector2(4f, 3f);
                rt.offsetMax = new Vector2(-4f, -3f);

                var capturedCustomer = c;
                var capturedRecruited = recruited;
                var btn = CreateButton(cell.transform, "ReassignBtn", assign + "  ▾",
                    () => OpenReassignPopup(capturedCustomer, capturedRecruited));
                FillParent(btn.GetComponent<RectTransform>());
            }
        }

        // =====================================================================
        // Reassign popup
        // =====================================================================

        private static void OpenReassignPopup(CustomerInfo c, List<DealerInfo> recruited)
        {
            CloseReassignPopup();

            float rowH = 56f;
            int rows = 1 /*title*/ + 1 /*player*/ + recruited.Count + 1 /*cancel*/;
            float height = 24f + rows * (rowH + 8f);

            _reassignPopupGO = NewUIGameObject("ReassignPopup");
            _reassignPopupGO.transform.SetParent(_optimizerPanelGO.transform, false);
            // Stretch popup width to ~90% of the panel so it fits inside the phone.
            var rt = _reassignPopupGO.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.05f, 0.5f);
            rt.anchorMax = new Vector2(0.95f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(0f, height);
            var bg = _reassignPopupGO.AddComponent<Image>();
            bg.color = new Color(0.06f, 0.06f, 0.08f, 0.98f);
            bg.raycastTarget = true;

            float y = -12f;
            var title = CreateText(_reassignPopupGO.transform, "Title",
                $"Move {c.FullName} to:", 26, TextAnchor.MiddleCenter);
            var titleRT = title.GetComponent<RectTransform>();
            titleRT.anchorMin = new Vector2(0f, 1f);
            titleRT.anchorMax = new Vector2(1f, 1f);
            titleRT.pivot = new Vector2(0.5f, 1f);
            titleRT.anchoredPosition = new Vector2(0f, y);
            titleRT.sizeDelta = new Vector2(-16f, rowH);
            var titleTxt = title.GetComponent<Text>();
            if (titleTxt != null) titleTxt.raycastTarget = false;
            y -= (rowH + 8f);

            var capturedC = c;
            var playerBtn = CreateButton(_reassignPopupGO.transform, "BtnPlayer", "Player",
                () => ReassignToPlayer(capturedC));
            PositionPopupButton(playerBtn, y, rowH);
            if (c.IsPlayerAssigned) SetButtonEnabled(playerBtn, false);
            y -= (rowH + 8f);

            foreach (var d in recruited)
            {
                var dealerName = d.FullName;
                bool isCurrent = !c.IsPlayerAssigned && string.Equals(c.AssignedDealerName, dealerName, StringComparison.Ordinal);
                bool isFull = d.AssignedCustomerCount >= 10;

                string label = isFull ? $"{dealerName}  (full)" : dealerName;
                var capturedName = dealerName;
                var btn = CreateButton(_reassignPopupGO.transform, $"Btn_{dealerName}", label,
                    () => ReassignToDealer(capturedC, capturedName));
                PositionPopupButton(btn, y, rowH);
                if (isCurrent || isFull) SetButtonEnabled(btn, false);
                y -= (rowH + 8f);
            }

            var cancel = CreateButton(_reassignPopupGO.transform, "BtnCancel", "Cancel",
                () => CloseReassignPopup());
            SetButtonColor(cancel, new Color(0.4f, 0.2f, 0.2f, 1f));
            PositionPopupButton(cancel, y, rowH);
        }

        private static void ReassignToPlayer(CustomerInfo c)
        {
            if (c.IsPlayerAssigned) { CloseReassignPopup(); return; }
            ReassignmentService.MoveToPlayer(c.NpcId);
            CloseReassignPopup();
            RefreshTable();
        }

        private static void ReassignToDealer(CustomerInfo c, string dealerName)
        {
            ReassignmentService.MoveToDealer(c.NpcId, dealerName);
            CloseReassignPopup();
            RefreshTable();
        }

        private static void PositionPopupButton(GameObject btnGO, float topOffset, float height)
        {
            var rt = btnGO.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, topOffset);
            rt.sizeDelta = new Vector2(-24f, height);
        }

        private static void CloseReassignPopup()
        {
            if (_reassignPopupGO != null)
            {
                GameObject.Destroy(_reassignPopupGO);
                _reassignPopupGO = null;
            }
        }

        // =====================================================================
        // Phone orientation
        // =====================================================================

        private static void SetPhoneHorizontal(bool horizontal)
        {
            try
            {
                if (!ResolvePhone()) return;
                var instance = _phoneInstanceProp.GetValue(null);
                if (instance == null) return;
                _phoneSetIsHorizontal.Invoke(instance, new object[] { horizontal });
            }
            catch (Exception ex)
            {
                ModLogger.Error($"[OptimizerTab] SetPhoneHorizontal failed: {ex.InnerException?.Message ?? ex.Message}");
            }
        }

        private static bool ResolvePhone()
        {
            if (_phoneResolved) return _phoneType != null && _phoneSetIsHorizontal != null && _phoneInstanceProp != null;

            // Phone.Instance lives on the generic base PlayerSingleton<T>; FlattenHierarchy
            // doesn't reliably cross generic-base statics in Il2CppInterop, so walk the chain.
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                _phoneType = asm.GetType("Il2CppScheduleOne.UI.Phone.Phone");
                if (_phoneType != null) break;
            }
            if (_phoneType == null) { _phoneResolved = true; return false; }

            var t = _phoneType;
            while (t != null && _phoneInstanceProp == null)
            {
                _phoneInstanceProp = t.GetProperty("Instance",
                    BindingFlags.Public | BindingFlags.Static);
                t = t.BaseType;
            }

            _phoneSetIsHorizontal = _phoneType.GetMethod("SetIsHorizontal",
                BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy,
                null, new[] { typeof(bool) }, null);

            _phoneResolved = true;
            return _phoneInstanceProp != null && _phoneSetIsHorizontal != null;
        }

        // =====================================================================
        // uGUI helpers
        // =====================================================================

        private static GameObject NewUIGameObject(string name)
        {
            var types = new Il2CppReferenceArray<Il2CppSystem.Type>(1);
            types[0] = Il2CppType.Of<RectTransform>();
            return new GameObject(name, types);
        }

        private static Font GetFont()
        {
            if (_cachedFont != null) return _cachedFont;

            try { _cachedFont = Resources.GetBuiltinResource<Font>("Arial.ttf"); } catch { }
            if (_cachedFont != null) return _cachedFont;
            try { _cachedFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf"); } catch { }
            if (_cachedFont != null) return _cachedFont;

            try
            {
                var texts = Resources.FindObjectsOfTypeAll<Text>();
                if (texts != null)
                {
                    foreach (var t in texts)
                    {
                        if (t != null && t.font != null) { _cachedFont = t.font; break; }
                    }
                }
            }
            catch { }

            return _cachedFont;
        }

        private static GameObject CreateText(Transform parent, string name, string label,
                                              int fontSize, TextAnchor align)
        {
            var go = NewUIGameObject(name);
            go.transform.SetParent(parent, false);
            var txt = go.AddComponent<Text>();
            txt.text = label;
            txt.font = GetFont();
            txt.fontSize = fontSize;
            txt.color = Color.white;
            txt.alignment = align;
            txt.horizontalOverflow = HorizontalWrapMode.Overflow;
            return go;
        }

        private static GameObject CreateButton(Transform parent, string name, string label, Action onClick)
        {
            var go = NewUIGameObject(name);
            go.transform.SetParent(parent, false);

            var img = go.AddComponent<Image>();
            img.color = new Color(0.25f, 0.45f, 0.75f, 1f);
            img.raycastTarget = true;

            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;

            var textGO = NewUIGameObject("Text");
            textGO.transform.SetParent(go.transform, false);
            FillParent(textGO.GetComponent<RectTransform>());
            var txt = textGO.AddComponent<Text>();
            txt.text = label;
            txt.font = GetFont();
            txt.fontSize = 24;
            txt.color = Color.white;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.horizontalOverflow = HorizontalWrapMode.Overflow;
            // The text inside a button must NOT swallow the button's click — the
            // button's Image is the raycast target, the inner Text should be transparent
            // to clicks so the click hits the parent Button via the Image.
            txt.raycastTarget = false;

            btn.onClick.AddListener((UnityAction)(() =>
            {
                try { onClick?.Invoke(); }
                catch (Exception ex) { ModLogger.Error($"[OptimizerTab] Button '{name}' handler: {ex.Message}"); }
            }));

            return go;
        }

        private static void SetButtonEnabled(GameObject buttonGO, bool enabled)
        {
            if (buttonGO == null) return;
            var btn = buttonGO.GetComponent<Button>();
            if (btn != null) btn.interactable = enabled;
            var img = buttonGO.GetComponent<Image>();
            if (img != null)
            {
                img.color = enabled
                    ? new Color(0.25f, 0.45f, 0.75f, 1f)
                    : new Color(0.30f, 0.30f, 0.32f, 0.6f);
            }
        }

        private static void SetButtonColor(GameObject buttonGO, Color color)
        {
            var img = buttonGO?.GetComponent<Image>();
            if (img != null) img.color = color;
        }

        private static void AnchorTopStretch(RectTransform rt, float topY, float height)
        {
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = new Vector2(0f, topY);
            rt.sizeDelta = new Vector2(-16f, height);
        }

        private static void FillParent(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private static void ClearChildren(Transform parent)
        {
            if (parent == null) return;
            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                var child = parent.GetChild(i);
                if (child != null) GameObject.Destroy(child.gameObject);
            }
        }

        private static void DisposeOwnedUI()
        {
            CloseReassignPopup();
            if (_optimizerPanelGO != null) { GameObject.Destroy(_optimizerPanelGO); _optimizerPanelGO = null; }
            if (_toggleButtonGO != null) { GameObject.Destroy(_toggleButtonGO); _toggleButtonGO = null; }
            _tableContentGO = null;
            _vanillaContentGO = null;
            _appContainer = null;
            _isOptimizerActive = false;
        }

        // =====================================================================
        // Reflection primitives
        // =====================================================================

        private static T ReflectGet<T>(object obj, string name) where T : Il2CppSystem.Object
        {
            if (obj == null) return null;
            try
            {
                var t = obj.GetType();
                object raw = null;

                var prop = t.GetProperty(name,
                    BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy);
                if (prop != null) raw = prop.GetValue(obj);

                if (raw == null)
                {
                    var field = t.GetField(name,
                        BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy);
                    if (field != null) raw = field.GetValue(obj);
                }

                if (raw == null) return null;

                // Il2CppInterop wraps returns as the property's declared type. Narrowing to a
                // more-derived type (e.g. Transform → RectTransform) needs TryCast, not `as`.
                if (raw is Il2CppObjectBase icb)
                {
                    return icb.TryCast<T>();
                }
                return raw as T;
            }
            catch (Exception ex)
            {
                ModLogger.Debug($"[OptimizerTab] ReflectGet<{typeof(T).Name}>({name}): {ex.Message}");
            }
            return null;
        }
    }
}
