using System;
using System.Collections.Generic;
using System.Linq;
using Azathrix.Framework.Core.Pipeline;
using Azathrix.Framework.Editor.Registry;
using UnityEditor;
using UnityEngine;

namespace Azathrix.Framework.Editor.Pipeline
{
    /// <summary>
    /// 管线注册表窗口 - 统一管理所有管线的阶段和钩子
    /// </summary>
    public class PipelineRegistryWindow : EditorWindow
    {
        private PipelineRegistry _registry;
        private Vector2 _scrollPos;
        private string _searchFilter = "";
        private bool _showOnlyDisabled;
        private bool _showOnlyEnabled;

        // 分组折叠状态
        private Dictionary<string, bool> _foldouts = new();

        // 排序
        private string _sortColumn;
        private bool _sortAscending = true;

        // Order 编辑状态
        private string _editingOrderKey;
        private int _editingOrderValue;

        private sealed class HookView
        {
            public HookEntry Hook;
            public string Target;
        }

        // 列定义
        private readonly float[] _colWidths = { 40f, 60f, 180f, 80f, 120f };
        private readonly string[] _colNames = { "启用", "Order", "名称", "类型", "接口" };

        [MenuItem("Azathrix/注册表/管线注册表")]
        public static void ShowWindow()
        {
            var window = GetWindow<PipelineRegistryWindow>("管线注册表");
            window.minSize = new Vector2(700, 400);
        }

        private void OnEnable()
        {
            _registry = PipelineRegistry.Instance;
            PipelineRegistry.OnRegistryChanged += Repaint;
        }

        private void OnDisable()
        {
            PipelineRegistry.OnRegistryChanged -= Repaint;
        }

        private void OnGUI()
        {
            if (_registry == null)
            {
                EditorGUILayout.HelpBox("无法加载管线注册表", MessageType.Error);
                return;
            }

            DrawToolbar();
            DrawHeader();

            _scrollPos = GUILayout.BeginScrollView(_scrollPos);
            DrawContent();
            GUILayout.EndScrollView();
        }

        private void DrawToolbar()
        {
            var totalPhases = _registry.pipelines.Sum(p => p.phases.Count(e => !e.IsMissing));
            var totalHooks = _registry.pipelines.Sum(p => p.hooks.Count(e => !e.IsMissing));
            var enabledPhases = _registry.pipelines.Sum(p => p.phases.Count(e => !e.IsMissing && e.enabled));
            var enabledHooks = _registry.pipelines.Sum(p => p.hooks.Count(e => !e.IsMissing && e.enabled));

            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            GUILayout.Label($"管线:{_registry.pipelines.Count}", GUILayout.Width(55));
            GUILayout.Label($"阶段:{enabledPhases}/{totalPhases}", GUILayout.Width(70));
            GUILayout.Label($"钩子:{enabledHooks}/{totalHooks}", GUILayout.Width(70));

            GUILayout.Space(10);

            GUILayout.Label("搜索:", GUILayout.Width(35));
            _searchFilter = EditorGUILayout.TextField(_searchFilter, EditorStyles.toolbarSearchField, GUILayout.Width(120));

            GUILayout.Space(5);

            _showOnlyDisabled = GUILayout.Toggle(_showOnlyDisabled, "仅禁用", EditorStyles.toolbarButton, GUILayout.Width(50));
            _showOnlyEnabled = GUILayout.Toggle(_showOnlyEnabled, "仅启用", EditorStyles.toolbarButton, GUILayout.Width(50));

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("刷新", EditorStyles.toolbarButton, GUILayout.Width(40)))
            {
                PipelineRegistryScanner.ScanAll();
                Repaint();
            }

            if (GUILayout.Button("清理", EditorStyles.toolbarButton, GUILayout.Width(40)))
            {
                CleanupMissingEntries();
            }

            EditorGUILayout.EndHorizontal();
        }

        private void DrawHeader()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            for (int i = 0; i < _colNames.Length; i++)
            {
                var style = _sortColumn == _colNames[i] ? EditorStyles.boldLabel : EditorStyles.label;
                var label = _colNames[i];
                if (_sortColumn == _colNames[i])
                    label += _sortAscending ? " ▲" : " ▼";

                var rect = GUILayoutUtility.GetRect(new GUIContent(label), style, GUILayout.Width(_colWidths[i]));

                if (Event.current.type == EventType.MouseDown && rect.Contains(Event.current.mousePosition))
                {
                    if (_sortColumn == _colNames[i])
                        _sortAscending = !_sortAscending;
                    else
                    {
                        _sortColumn = _colNames[i];
                        _sortAscending = true;
                    }
                    Event.current.Use();
                    Repaint();
                }

                GUI.Label(rect, label, style);
            }

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }

        private void DrawContent()
        {
            foreach (var pipeline in _registry.pipelines.OrderBy(p => p.pipelineId))
            {
                DrawPipelineGroup(pipeline);
            }
        }

        private void DrawPipelineGroup(PipelineEntry pipeline)
        {
            var key = $"pipeline_{pipeline.pipelineId}";
            if (!_foldouts.ContainsKey(key))
                _foldouts[key] = true;

            var validPhases = pipeline.phases.Where(p => !p.IsMissing).ToList();
            var validHooks = pipeline.hooks.Where(h => !h.IsMissing).ToList();
            var totalCount = validPhases.Count + validHooks.Count;
            var enabledCount = validPhases.Count(p => p.enabled) + validHooks.Count(h => h.enabled);

            // 如果没有有效条目，不显示这个管线
            if (totalCount == 0) return;

            // 管线组头
            var rect = EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            var bgColor = rect.Contains(Event.current.mousePosition) ? new Color(0.28f, 0.28f, 0.28f) : new Color(0.15f, 0.15f, 0.15f);
            EditorGUI.DrawRect(rect, bgColor);

            if (Event.current.type == EventType.MouseDown && rect.Contains(Event.current.mousePosition))
            {
                if (Event.current.mousePosition.x < rect.xMax - 80)
                {
                    _foldouts[key] = !_foldouts[key];
                    Event.current.Use();
                }
            }

            _foldouts[key] = EditorGUILayout.Foldout(_foldouts[key], $"{pipeline.displayName} ({enabledCount}/{totalCount})", true);

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("All", EditorStyles.toolbarButton, GUILayout.Width(30)))
            {
                foreach (var p in pipeline.phases) p.enabled = true;
                foreach (var h in pipeline.hooks) h.enabled = true;
                EditorUtility.SetDirty(_registry);
            }
            if (GUILayout.Button("None", EditorStyles.toolbarButton, GUILayout.Width(40)))
            {
                foreach (var p in pipeline.phases) p.enabled = false;
                foreach (var h in pipeline.hooks) h.enabled = false;
                EditorUtility.SetDirty(_registry);
            }

            EditorGUILayout.EndHorizontal();

            if (!_foldouts[key]) return;

            // 绘制阶段和对应的钩子
            var filteredPhases = FilterPhases(pipeline.phases);
            var sortedPhases = ApplySort(filteredPhases);
            var filteredHooks = FilterHooks(pipeline.hooks);
            var hookViews = ExpandHooks(filteredHooks);
            var rowIndex = 0;

            for (int i = 0; i < sortedPhases.Count; i++)
            {
                var phase = sortedPhases[i];

                // 绘制该阶段的钩子（前置在前，后置在后）
                var phaseHooks = hookViews
                    .Where(h => MatchesPhaseTarget(phase, h.Target))
                    .ToList();

                var beforeHooks = phaseHooks
                    .Where(h => h.Hook.isBefore)
                    .OrderBy(h => h.Hook.order)
                    .ToList();

                var afterHooks = phaseHooks
                    .Where(h => !h.Hook.isBefore)
                    .OrderBy(h => h.Hook.order)
                    .ToList();

                foreach (var hook in beforeHooks)
                    DrawHookRow(pipeline, hook.Hook, hook.Target, rowIndex++);

                DrawPhaseRow(pipeline, phase, rowIndex++);

                foreach (var hook in afterHooks)
                    DrawHookRow(pipeline, hook.Hook, hook.Target, rowIndex++);
            }

            // 绘制没有匹配阶段的钩子
            var orphanHooks = hookViews
                .Where(h => !sortedPhases.Any(p => MatchesPhaseTarget(p, h.Target)))
                .OrderBy(h => h.Hook.isBefore ? 0 : 1)
                .ThenBy(h => h.Hook.order)
                .ToList();

            foreach (var hook in orphanHooks)
                DrawHookRow(pipeline, hook.Hook, hook.Target, rowIndex++);
        }

        private void DrawPhaseRow(PipelineEntry pipeline, PhaseEntry phase, int index)
        {
            var bgColor = index % 2 == 0 ? new Color(0.18f, 0.18f, 0.18f) : new Color(0.22f, 0.22f, 0.22f);

            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(16);

            var rect = EditorGUILayout.BeginHorizontal();
            EditorGUI.DrawRect(rect, bgColor);

            if (!phase.enabled) GUI.color = Color.gray;

            // 启用
            EditorGUI.BeginChangeCheck();
            phase.enabled = EditorGUILayout.Toggle(phase.enabled, GUILayout.Width(_colWidths[0]));
            if (EditorGUI.EndChangeCheck())
                EditorUtility.SetDirty(_registry);

            // Order
            DrawOrderCell(phase, _colWidths[1]);

            // 名称
            EditorGUILayout.LabelField(phase.displayName, GUILayout.Width(_colWidths[2]));

            GUI.color = phase.enabled ? Color.white : Color.gray;

            // 类型
            EditorGUILayout.LabelField("阶段", GUILayout.Width(_colWidths[3]));

            // 接口
            var interfaceName = phase.interfaceTypeName?.Split('.').Last() ?? "-";
            EditorGUILayout.LabelField(interfaceName, GUILayout.Width(_colWidths[4]));

            GUI.color = Color.white;

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndHorizontal();
        }

        private void DrawHookRow(PipelineEntry pipeline, HookEntry hook, string target, int index)
        {
            // 钩子使用偏蓝色调
            var bgColor = index % 2 == 0 ? new Color(0.15f, 0.17f, 0.22f) : new Color(0.18f, 0.20f, 0.25f);

            EditorGUILayout.BeginHorizontal();
            GUILayout.Space(16);

            var rect = EditorGUILayout.BeginHorizontal();
            EditorGUI.DrawRect(rect, bgColor);

            if (!hook.enabled) GUI.color = Color.gray;

            // 启用
            EditorGUI.BeginChangeCheck();
            hook.enabled = EditorGUILayout.Toggle(hook.enabled, GUILayout.Width(_colWidths[0]));
            if (EditorGUI.EndChangeCheck())
                EditorUtility.SetDirty(_registry);

            // Order
            DrawHookOrderCell(hook, _colWidths[1]);

            // 名称
            EditorGUILayout.LabelField(hook.displayName, GUILayout.Width(_colWidths[2]));

            // 类型
            var timing = hook.isBefore ? "前置钩子" : "后置钩子";
            EditorGUILayout.LabelField(timing, GUILayout.Width(_colWidths[3]));

            // 目标
            var targetName = string.IsNullOrEmpty(target) ? "-" : target.Split('.').Last();
            EditorGUILayout.LabelField(targetName, GUILayout.Width(_colWidths[4]));

            GUI.color = Color.white;

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndHorizontal();
        }

        private void DrawOrderCell(PhaseEntry phase, float width)
        {
            var key = $"phase_{phase.typeName}";
            var isEditing = _editingOrderKey == key;
            var rect = GUILayoutUtility.GetRect(width, 18f, GUILayout.Width(width));
            var isHover = rect.Contains(Event.current.mousePosition);
            var hasCustom = phase.hasCustomOrder;

            if (isEditing)
            {
                var inputRect = new Rect(rect.x, rect.y, rect.width - 22, rect.height);
                var btnRect = new Rect(rect.x + rect.width - 20, rect.y, 18, rect.height);

                _editingOrderValue = EditorGUI.IntField(inputRect, _editingOrderValue);

                if (GUI.Button(btnRect, "✓", EditorStyles.miniButton))
                {
                    phase.order = _editingOrderValue;
                    phase.hasCustomOrder = _editingOrderValue != phase.defaultOrder;
                    EditorUtility.SetDirty(_registry);
                    _editingOrderKey = null;
                }

                if (Event.current.type == EventType.MouseDown && !rect.Contains(Event.current.mousePosition))
                {
                    _editingOrderKey = null;
                    Event.current.Use();
                }
            }
            else
            {
                var displayRect = hasCustom && isHover ? new Rect(rect.x, rect.y, rect.width - 28, rect.height) : rect;

                if (isHover)
                {
                    EditorGUI.DrawRect(displayRect, new Color(0.3f, 0.3f, 0.3f, 0.5f));
                    EditorGUIUtility.AddCursorRect(displayRect, MouseCursor.Text);
                }

                // 显示文本
                var displayText = hasCustom ? $"{phase.order}({phase.defaultOrder})" : phase.order.ToString();
                var style = hasCustom ? EditorStyles.boldLabel : EditorStyles.label;
                EditorGUI.LabelField(displayRect, displayText, style);

                // 还原按钮
                if (hasCustom && isHover)
                {
                    var resetRect = new Rect(rect.xMax - 26, rect.y, 24, rect.height);
                    if (GUI.Button(resetRect, "↺"))
                    {
                        phase.order = phase.defaultOrder;
                        phase.hasCustomOrder = false;
                        EditorUtility.SetDirty(_registry);
                    }
                }

                if (Event.current.type == EventType.MouseDown && displayRect.Contains(Event.current.mousePosition))
                {
                    _editingOrderKey = key;
                    _editingOrderValue = phase.order;
                    Event.current.Use();
                }
            }

            if (isHover && Event.current.type == EventType.MouseMove)
                Repaint();
        }

        private void DrawHookOrderCell(HookEntry hook, float width)
        {
            var key = $"hook_{hook.typeName}";
            var isEditing = _editingOrderKey == key;
            var rect = GUILayoutUtility.GetRect(width, 18f, GUILayout.Width(width));
            var isHover = rect.Contains(Event.current.mousePosition);
            var hasCustom = hook.hasCustomOrder;

            if (isEditing)
            {
                var inputRect = new Rect(rect.x, rect.y, rect.width - 22, rect.height);
                var btnRect = new Rect(rect.x + rect.width - 20, rect.y, 18, rect.height);

                _editingOrderValue = EditorGUI.IntField(inputRect, _editingOrderValue);

                if (GUI.Button(btnRect, "✓", EditorStyles.miniButton))
                {
                    hook.order = _editingOrderValue;
                    hook.hasCustomOrder = _editingOrderValue != hook.defaultOrder;
                    EditorUtility.SetDirty(_registry);
                    _editingOrderKey = null;
                }

                if (Event.current.type == EventType.MouseDown && !rect.Contains(Event.current.mousePosition))
                {
                    _editingOrderKey = null;
                    Event.current.Use();
                }
            }
            else
            {
                var displayRect = hasCustom && isHover ? new Rect(rect.x, rect.y, rect.width - 28, rect.height) : rect;

                if (isHover)
                {
                    EditorGUI.DrawRect(displayRect, new Color(0.3f, 0.3f, 0.3f, 0.5f));
                    EditorGUIUtility.AddCursorRect(displayRect, MouseCursor.Text);
                }

                // 显示文本
                var displayText = hasCustom ? $"{hook.order}({hook.defaultOrder})" : hook.order.ToString();
                var style = hasCustom ? EditorStyles.boldLabel : EditorStyles.label;
                EditorGUI.LabelField(displayRect, displayText, style);

                // 还原按钮
                if (hasCustom && isHover)
                {
                    var resetRect = new Rect(rect.xMax - 26, rect.y, 24, rect.height);
                    if (GUI.Button(resetRect, "↺"))
                    {
                        hook.order = hook.defaultOrder;
                        hook.hasCustomOrder = false;
                        EditorUtility.SetDirty(_registry);
                    }
                }

                if (Event.current.type == EventType.MouseDown && displayRect.Contains(Event.current.mousePosition))
                {
                    _editingOrderKey = key;
                    _editingOrderValue = hook.order;
                    Event.current.Use();
                }
            }

            if (isHover && Event.current.type == EventType.MouseMove)
                Repaint();
        }

        private List<PhaseEntry> FilterPhases(List<PhaseEntry> phases)
        {
            return phases.Where(p =>
            {
                // 过滤丢失的类型
                if (p.IsMissing) return false;

                if (!string.IsNullOrEmpty(_searchFilter))
                {
                    var filter = _searchFilter.ToLower();
                    if (!p.displayName.ToLower().Contains(filter) && !p.typeName.ToLower().Contains(filter))
                        return false;
                }
                if (_showOnlyDisabled && p.enabled) return false;
                if (_showOnlyEnabled && !p.enabled) return false;
                return true;
            }).ToList();
        }

        private List<HookEntry> FilterHooks(List<HookEntry> hooks)
        {
            return hooks.Where(h =>
            {
                // 过滤丢失的类型
                if (h.IsMissing) return false;

                if (!string.IsNullOrEmpty(_searchFilter))
                {
                    var filter = _searchFilter.ToLower();
                    if (!h.displayName.ToLower().Contains(filter) && !h.typeName.ToLower().Contains(filter))
                        return false;
                }
                if (_showOnlyDisabled && h.enabled) return false;
                if (_showOnlyEnabled && !h.enabled) return false;
                return true;
            }).ToList();
        }

        private static List<HookView> ExpandHooks(List<HookEntry> hooks)
        {
            var views = new List<HookView>();
            foreach (var hook in hooks)
            {
                if (hook.targets == null || hook.targets.Count == 0)
                    continue;

                foreach (var target in hook.targets)
                {
                    if (string.IsNullOrEmpty(target.phaseId))
                        continue;
                    views.Add(new HookView { Hook = hook, Target = target.phaseId });
                }
            }

            return views;
        }

        private static bool MatchesPhaseTarget(PhaseEntry phase, string target)
        {
            if (string.IsNullOrEmpty(target))
                return false;

            return string.Equals(target, phase.phaseId, StringComparison.OrdinalIgnoreCase);
        }

        private List<PhaseEntry> ApplySort(List<PhaseEntry> phases)
        {
            if (string.IsNullOrEmpty(_sortColumn))
                return phases.OrderBy(p => p.order).ToList();

            IEnumerable<PhaseEntry> sorted = _sortColumn switch
            {
                "启用" => _sortAscending ? phases.OrderBy(p => p.enabled) : phases.OrderByDescending(p => p.enabled),
                "Order" => _sortAscending ? phases.OrderBy(p => p.order) : phases.OrderByDescending(p => p.order),
                "名称" => _sortAscending ? phases.OrderBy(p => p.displayName) : phases.OrderByDescending(p => p.displayName),
                _ => phases.OrderBy(p => p.order)
            };

            return sorted.ToList();
        }

        private void CleanupMissingEntries()
        {
            var removedPhases = 0;
            var removedHooks = 0;
            var removedPipelines = 0;

            foreach (var pipeline in _registry.pipelines.ToList())
            {
                removedPhases += pipeline.phases.RemoveAll(p => p.IsMissing);
                removedHooks += pipeline.hooks.RemoveAll(h => h.IsMissing);

                // 如果管线类型也丢失且没有任何阶段和钩子，移除整个管线
                if (pipeline.GetPipelineType() == null && pipeline.phases.Count == 0 && pipeline.hooks.Count == 0)
                {
                    _registry.pipelines.Remove(pipeline);
                    removedPipelines++;
                }
            }

            if (removedPhases > 0 || removedHooks > 0 || removedPipelines > 0)
            {
                _registry.ClearCache();
                EditorUtility.SetDirty(_registry);
                AssetDatabase.SaveAssets();
                Debug.Log($"[PipelineRegistry] 清理完成: 移除 {removedPipelines} 个管线, {removedPhases} 个阶段, {removedHooks} 个钩子");
            }
            else
            {
                Debug.Log("[PipelineRegistry] 没有需要清理的数据");
            }
        }
    }
}
