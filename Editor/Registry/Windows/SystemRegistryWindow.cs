using System;
using System.Collections.Generic;
using System.Linq;
using Azathrix.Framework.Registry;
using UnityEditor;
using UnityEngine;

namespace Azathrix.Framework.Editor.Registry
{
    public class SystemRegistryWindow : RegistryWindowBase<SystemRegistry, SystemEntry>
    {
        protected override string WindowTitle => "系统注册表";

        // 用于显示的统一行数据
        private class RowData
        {
            public string type;        // "系统" 或 "接口"
            public string name;
            public string interfaces;   // 实现的接口
            public string dependencies;
            public string assembly;
            public bool enabled;
            public SystemEntry entry;  // 系统条目
            public InterfaceEntry interfaceEntry; // 接口条目
            public List<SystemEntry> implementations; // 接口的实现列表
        }

        // 优先级编辑状态
        private string _editingPriorityKey;
        private int _editingPriorityValue;

        [MenuItem("Azathrix/注册表/系统注册表")]
        public static void ShowWindow()
        {
            var window = GetWindow<SystemRegistryWindow>("系统注册表");
            window.minSize = new Vector2(600, 400);
        }

        protected override void OnEnable()
        {
            columns = new List<ColumnDef>
            {
                new() { name = "启用", width = 50, sortable = true },
                new() { name = "优先级", width = 90, sortable = true },
                new() { name = "类型", width = 60, sortable = true },
                new() { name = "名称", width = 180, sortable = true },
                new() { name = "实现", width = 150, sortable = false },
                new() { name = "依赖", width = 120, sortable = false },
            };
            base.OnEnable();
        }

        protected override SystemRegistry GetRegistry() => SystemRegistry.Instance;

        protected override void DrawContent(SystemRegistry registry)
        {
            // 构建行数据：系统 + 接口
            var rows = BuildRows(registry);

            // 按程序集分组
            var groups = rows
                .GroupBy(r => r.assembly ?? "Unknown")
                .OrderBy(g => g.Key);

            foreach (var group in groups)
            {
                DrawGroup(registry, group.Key, group.ToList());
            }
        }

        private List<RowData> BuildRows(SystemRegistry registry)
        {
            var rows = new List<RowData>();

            // 添加系统行
            foreach (var entry in registry.entries)
            {
                if (!MatchesFilter(entry.displayName, entry.typeName)) continue;
                if (!PassesEnabledFilter(entry.enabled)) continue;

                var interfaces = entry.interfaces.Count > 0
                    ? string.Join(", ", entry.interfaces.Select(i => i.Split('.').Last()))
                    : "-";

                var deps = entry.dependencies.Count > 0
                    ? string.Join(", ", entry.dependencies.Select(d => d.Split('.').Last()))
                    : "-";

                rows.Add(new RowData
                {
                    type = "系统",
                    name = entry.displayName,
                    interfaces = interfaces,
                    dependencies = deps,
                    assembly = entry.assemblyName,
                    enabled = entry.enabled,
                    entry = entry
                });
            }

            // 添加接口行（从注册表读取）
            foreach (var ifaceEntry in registry.interfaceEntries)
            {
                if (!MatchesFilter(ifaceEntry.displayName, ifaceEntry.typeName)) continue;
                if (!PassesEnabledFilter(ifaceEntry.enabled)) continue;

                var implementations = registry.entries
                    .Where(e => ifaceEntry.implementations.Contains(e.typeName))
                    .ToList();

                rows.Add(new RowData
                {
                    type = "接口",
                    name = ifaceEntry.displayName,
                    interfaces = "-",
                    dependencies = $"实现: {implementations.Count}",
                    assembly = ifaceEntry.assemblyName,
                    enabled = ifaceEntry.enabled,
                    interfaceEntry = ifaceEntry,
                    implementations = implementations
                });
            }

            // 排序
            rows = ApplySorting(rows);

            return rows;
        }

        // 获取系统的有效优先级（考虑接口）
        private int GetSystemEffectivePriority(SystemRegistry registry, SystemEntry entry)
        {
            // 系统自定义 > 系统默认 > 接口自定义 > 接口默认 > 0
            if (entry.hasCustomPriority) return entry.priority;
            if (entry.defaultPriority != 0) return entry.defaultPriority;

            // 查找接口优先级
            foreach (var ifaceTypeName in entry.interfaces)
            {
                var ifaceEntry = registry.GetInterfaceEntry(ifaceTypeName);
                if (ifaceEntry != null)
                {
                    if (ifaceEntry.hasCustomPriority) return ifaceEntry.priority;
                    if (ifaceEntry.defaultPriority != 0) return ifaceEntry.defaultPriority;
                }
            }
            return 0;
        }

        // 获取系统的回退优先级（用于显示）
        private int GetSystemFallbackPriority(SystemRegistry registry, SystemEntry entry)
        {
            // 返回接口的优先级（如果有）
            foreach (var ifaceTypeName in entry.interfaces)
            {
                var ifaceEntry = registry.GetInterfaceEntry(ifaceTypeName);
                if (ifaceEntry != null)
                {
                    return ifaceEntry.EffectivePriority;
                }
            }
            return 0;
        }

        private List<RowData> ApplySorting(List<RowData> rows)
        {
            if (string.IsNullOrEmpty(sortColumn)) return rows;

            var registry = GetRegistry();
            IEnumerable<RowData> sorted = sortColumn switch
            {
                "启用" => sortAscending ? rows.OrderBy(r => r.enabled) : rows.OrderByDescending(r => r.enabled),
                "优先级" => sortAscending
                    ? rows.OrderBy(r => r.entry != null ? GetSystemEffectivePriority(registry, r.entry) : r.interfaceEntry?.EffectivePriority ?? 0)
                    : rows.OrderByDescending(r => r.entry != null ? GetSystemEffectivePriority(registry, r.entry) : r.interfaceEntry?.EffectivePriority ?? 0),
                "类型" => sortAscending ? rows.OrderBy(r => r.type) : rows.OrderByDescending(r => r.type),
                "名称" => sortAscending ? rows.OrderBy(r => r.name) : rows.OrderByDescending(r => r.name),
                _ => rows
            };

            return sorted.ToList();
        }

        private void DrawGroup(SystemRegistry registry, string groupName, List<RowData> rows)
        {
            var key = $"sys_{groupName}";
            var enabledCount = rows.Count(r => r.enabled);
            var systemEntries = rows.Where(r => r.entry != null).Select(r => r.entry).ToList();
            var interfaceEntries = rows.Where(r => r.interfaceEntry != null).Select(r => r.interfaceEntry).ToList();

            // 分组头
            BeginGroupHeader(key, groupName, enabledCount, rows.Count);

            GUILayout.FlexibleSpace();

            GUI.contentColor = new Color(0.8f, 0.8f, 0.8f);
            if (GUILayout.Button("All", EditorStyles.toolbarButton, GUILayout.Width(30)))
            {
                foreach (var e in systemEntries) e.enabled = true;
                foreach (var e in interfaceEntries) e.enabled = true;
                EditorUtility.SetDirty(registry);
            }
            if (GUILayout.Button("None", EditorStyles.toolbarButton, GUILayout.Width(40)))
            {
                foreach (var e in systemEntries) e.enabled = false;
                foreach (var e in interfaceEntries) e.enabled = false;
                EditorUtility.SetDirty(registry);
            }
            GUI.contentColor = Color.white;

            EditorGUILayout.EndHorizontal();

            if (!groupFoldouts[key]) return;

            // 条目列表
            for (int i = 0; i < rows.Count; i++)
            {
                DrawRow(registry, rows[i], i);
            }
        }

        private void DrawRow(SystemRegistry registry, RowData row, int index)
        {
            BeginRow(row.enabled, index);

            if (row.type == "系统")
            {
                // 系统行
                DrawEnableToggle(registry, row.entry, columns[0].width + SplitterWidth);

                // 优先级 - 系统
                var fallbackPriority = GetSystemFallbackPriority(registry, row.entry);
                DrawSystemPriorityCell(registry, row.entry, fallbackPriority, columns[1].width + SplitterWidth);

                // 类型
                GUI.color = row.enabled ? new Color(0.6f, 0.9f, 0.6f) : Color.gray;
                EditorGUILayout.LabelField("[系统]", GUILayout.Width(columns[2].width + SplitterWidth));
                GUI.color = row.enabled ? Color.white : Color.gray;

                // 名称
                var nameLabel = row.entry.isDefault ? $"{row.name} [默认]" : row.name;
                EditorGUILayout.LabelField(nameLabel, GUILayout.Width(columns[3].width + SplitterWidth));

                // 实现
                EditorGUILayout.LabelField(row.interfaces, GUILayout.Width(columns[4].width + SplitterWidth));

                // 依赖
                EditorGUILayout.LabelField(row.dependencies);
            }
            else
            {
                // 接口行 - 检查实现状态
                var implStatus = GetImplementationStatus(registry, row);

                // 根据状态设置整行颜色（在绘制任何内容之前）
                Color rowColor;
                if (!row.enabled)
                    rowColor = Color.gray;
                else if (implStatus == ImplStatus.Missing)
                    rowColor = new Color(1f, 0.5f, 0.5f);
                else if (implStatus == ImplStatus.Disabled)
                    rowColor = new Color(1f, 0.85f, 0.5f);
                else
                    rowColor = Color.white;

                GUI.color = rowColor;

                // 接口行 - 启用开关
                DrawInterfaceEnableToggle(registry, row.interfaceEntry, columns[0].width + SplitterWidth);

                // 优先级 - 接口
                DrawInterfacePriorityCell(registry, row.interfaceEntry, columns[1].width + SplitterWidth);

                // 类型 - 正常时蓝色，有警告状态时使用行颜色
                GUI.color = (row.enabled && implStatus == ImplStatus.Normal) ? new Color(0.6f, 0.8f, 1f) : rowColor;
                EditorGUILayout.LabelField("[接口]", GUILayout.Width(columns[2].width + SplitterWidth));
                GUI.color = rowColor;

                // 名称
                EditorGUILayout.LabelField(row.name, GUILayout.Width(columns[3].width + SplitterWidth));

                // 实现列
                DrawImplementationSelector(registry, row, implStatus);

                // 依赖列显示实现数量
                EditorGUILayout.LabelField($"实现: {row.implementations.Count}");
            }

            EndRow();
        }

        private enum ImplStatus { Normal, Disabled, Missing }

        private ImplStatus GetImplementationStatus(SystemRegistry registry, RowData row)
        {
            var selectedImpl = registry.GetSelectedImplementation(row.interfaceEntry.typeName);

            // 如果没有配置，找当前实际使用的实现（默认或第一个）
            if (string.IsNullOrEmpty(selectedImpl) && row.implementations.Count > 0)
            {
                var defaultImpl = row.implementations.FirstOrDefault(e => e.isDefault) ?? row.implementations.First();
                if (!defaultImpl.enabled)
                    return ImplStatus.Disabled;
                return ImplStatus.Normal;
            }

            if (string.IsNullOrEmpty(selectedImpl))
                return ImplStatus.Normal;

            var selectedEntry = registry.entries.FirstOrDefault(e => e.typeName == selectedImpl);
            if (selectedEntry == null)
                return ImplStatus.Missing;
            if (!selectedEntry.enabled)
                return ImplStatus.Disabled;
            return ImplStatus.Normal;
        }

        private void DrawImplementationSelector(SystemRegistry registry, RowData row, ImplStatus status)
        {
            var selectedImpl = registry.GetSelectedImplementation(row.interfaceEntry.typeName);
            var width = columns[4].width + SplitterWidth;

            if (status == ImplStatus.Missing)
            {
                var missingName = selectedImpl?.Split('.').Last() ?? "?";
                EditorGUILayout.LabelField($"⚠ {missingName} [丢失]", GUILayout.Width(width));
            }
            else if (row.implementations.Count > 1)
            {
                var options = row.implementations.Select(e =>
                {
                    var label = e.displayName;
                    if (e.isDefault) label += " [默认]";
                    if (!e.enabled) label += " [禁用]";
                    return label;
                }).ToArray();

                var currentIndex = row.implementations.FindIndex(e => e.typeName == selectedImpl);
                var defaultIndex = row.implementations.FindIndex(e => e.isDefault);
                if (defaultIndex < 0) defaultIndex = 0;
                if (currentIndex < 0) currentIndex = defaultIndex;

                var newIndex = EditorGUILayout.Popup(currentIndex, options, GUILayout.Width(width));
                if (newIndex != currentIndex && newIndex >= 0 && newIndex < row.implementations.Count)
                {
                    var newTypeName = row.implementations[newIndex].typeName;
                    var existing = registry.interfaceSelections.FirstOrDefault(s => s.interfaceTypeName == row.interfaceEntry.typeName);
                    if (existing != null)
                        existing.selectedImplementation = newTypeName;
                    else
                    {
                        registry.interfaceSelections.Add(new InterfaceSelection
                        {
                            interfaceTypeName = row.interfaceEntry.typeName,
                            selectedImplementation = newTypeName
                        });
                    }
                    registry.ClearSelectionCache();
                    EditorUtility.SetDirty(registry);
                    Repaint();
                }
            }
            else if (row.implementations.Count == 1)
            {
                var impl = row.implementations.First();
                var label = impl.displayName;
                if (!impl.enabled) label += " [禁用]";
                EditorGUILayout.LabelField(label, GUILayout.Width(width));
            }
            else
            {
                EditorGUILayout.LabelField("-", GUILayout.Width(width));
            }
        }

        private void DrawSystemPriorityCell(SystemRegistry registry, SystemEntry entry, int fallbackPriority, float width)
        {
            var key = entry.typeName;
            var isEditing = _editingPriorityKey == key;
            var rect = GUILayoutUtility.GetRect(width, 18f, GUILayout.Width(width));
            var isHover = rect.Contains(Event.current.mousePosition);

            // 计算显示值和默认值
            int displayValue;
            int defaultValue = entry.defaultPriority != 0 ? entry.defaultPriority : fallbackPriority;
            bool hasCustom = entry.hasCustomPriority;

            if (hasCustom)
                displayValue = entry.priority;
            else if (entry.defaultPriority != 0)
                displayValue = entry.defaultPriority;
            else
                displayValue = fallbackPriority;

            if (isEditing)
            {
                DrawPriorityEditMode(registry, rect, defaultValue, () =>
                {
                    entry.priority = _editingPriorityValue;
                    entry.hasCustomPriority = _editingPriorityValue != defaultValue;
                });
            }
            else
            {
                DrawPriorityDisplayMode(rect, displayValue, defaultValue, hasCustom, isHover, key);

                // 还原按钮
                if (hasCustom && isHover)
                {
                    var resetRect = new Rect(rect.xMax - 26, rect.y, 24, rect.height);
                    if (GUI.Button(resetRect, "↺"))
                    {
                        entry.hasCustomPriority = false;
                        entry.priority = 0;
                        EditorUtility.SetDirty(registry);
                    }
                }
            }

            if (isHover && Event.current.type == EventType.MouseMove)
                Repaint();
        }

        private void DrawInterfacePriorityCell(SystemRegistry registry, InterfaceEntry entry, float width)
        {
            var key = entry.typeName;
            var isEditing = _editingPriorityKey == key;
            var rect = GUILayoutUtility.GetRect(width, 18f, GUILayout.Width(width));
            var isHover = rect.Contains(Event.current.mousePosition);

            int displayValue = entry.EffectivePriority;
            int defaultValue = entry.defaultPriority;
            bool hasCustom = entry.hasCustomPriority;

            if (isEditing)
            {
                DrawPriorityEditMode(registry, rect, defaultValue, () =>
                {
                    entry.priority = _editingPriorityValue;
                    entry.hasCustomPriority = _editingPriorityValue != defaultValue;
                });
            }
            else
            {
                DrawPriorityDisplayMode(rect, displayValue, defaultValue, hasCustom, isHover, key);

                // 还原按钮
                if (hasCustom && isHover)
                {
                    var resetRect = new Rect(rect.xMax - 26, rect.y, 24, rect.height);
                    if (GUI.Button(resetRect, "↺"))
                    {
                        entry.hasCustomPriority = false;
                        entry.priority = 0;
                        EditorUtility.SetDirty(registry);
                    }
                }
            }

            if (isHover && Event.current.type == EventType.MouseMove)
                Repaint();
        }

        private void DrawPriorityEditMode(SystemRegistry registry, Rect rect, int defaultValue, Action onApply)
        {
            var inputWidth = rect.width - 22;
            var inputRect = new Rect(rect.x, rect.y, inputWidth, rect.height);
            var btnRect = new Rect(rect.x + inputWidth + 2, rect.y, 18, rect.height);

            _editingPriorityValue = EditorGUI.IntField(inputRect, _editingPriorityValue);

            if (GUI.Button(btnRect, "✓", EditorStyles.miniButton))
            {
                onApply();
                EditorUtility.SetDirty(registry);
                _editingPriorityKey = null;
            }

            // 点击外部取消编辑
            if (Event.current.type == EventType.MouseDown && !rect.Contains(Event.current.mousePosition))
            {
                _editingPriorityKey = null;
                Event.current.Use();
            }
        }

        private void DrawPriorityDisplayMode(Rect rect, int displayValue, int defaultValue, bool hasCustom, bool isHover, string key)
        {
            // 计算显示区域（有还原按钮时需要缩短）
            var displayRect = hasCustom && isHover ? new Rect(rect.x, rect.y, rect.width - 28, rect.height) : rect;

            if (isHover)
            {
                EditorGUI.DrawRect(displayRect, new Color(0.3f, 0.3f, 0.3f, 0.5f));
                EditorGUIUtility.AddCursorRect(displayRect, MouseCursor.Text);
            }

            // 显示文本
            string displayText;
            if (hasCustom && displayValue != defaultValue)
            {
                displayText = $"{displayValue}({defaultValue})";
            }
            else
            {
                displayText = displayValue.ToString();
            }

            var baseStyle = hasCustom ? EditorStyles.boldLabel : EditorStyles.label;

            // 使用裁剪样式
            var style = new GUIStyle(baseStyle) { clipping = TextClipping.Clip };
            EditorGUI.LabelField(displayRect, displayText, style);

            // 点击进入编辑
            if (Event.current.type == EventType.MouseDown && rect.Contains(Event.current.mousePosition))
            {
                // 排除还原按钮区域
                if (!hasCustom || Event.current.mousePosition.x < rect.xMax - 26)
                {
                    _editingPriorityKey = key;
                    _editingPriorityValue = displayValue;
                    Event.current.Use();
                }
            }
        }

        private void DrawInterfaceEnableToggle(SystemRegistry registry, InterfaceEntry entry, float width)
        {
            var newEnabled = EditorGUILayout.Toggle(entry.enabled, GUILayout.Width(width));
            if (newEnabled != entry.enabled)
            {
                entry.enabled = newEnabled;
                EditorUtility.SetDirty(registry);
                Repaint();
            }
        }
    }
}
