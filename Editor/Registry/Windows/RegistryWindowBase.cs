using System;
using System.Collections.Generic;
using System.Linq;
using Azathrix.Framework.Registry;
using UnityEditor;
using UnityEngine;

namespace Azathrix.Framework.Editor.Registry
{
    public abstract class RegistryWindowBase<TRegistry, TEntry> : EditorWindow
        where TRegistry : RegistryBase<TRegistry, TEntry>
        where TEntry : RegistryEntryBase
    {
        protected Vector2 scrollPos;
        protected string searchFilter = "";
        protected bool showOnlyDisabled;
        protected bool showOnlyEnabled;

        // 列宽
        protected List<ColumnDef> columns = new();
        protected const float SplitterWidth = 4f;
        protected const float MinColWidth = 40f;

        // 拖拽状态
        private int _draggingCol = -1;
        private float _dragStartX;
        private float _dragStartWidth;

        // 分组折叠
        protected Dictionary<string, bool> groupFoldouts = new();

        // 排序
        protected string sortColumn;
        protected bool sortAscending = true;

        protected struct ColumnDef
        {
            public string name;
            public float width;
            public bool sortable;
        }

        protected abstract TRegistry GetRegistry();
        protected abstract void DrawContent(TRegistry registry);
        protected abstract string WindowTitle { get; }

        protected virtual void OnEnable()
        {
            wantsMouseMove = true;
            var registry = GetRegistry();
            if (registry != null)
            {
                RegistryBase<TRegistry, TEntry>.OnRegistryChanged += Repaint;
            }
            LoadColumnWidths();
        }

        protected virtual void OnDisable()
        {
            RegistryBase<TRegistry, TEntry>.OnRegistryChanged -= Repaint;
        }

        protected void OnGUI()
        {
            var registry = GetRegistry();
            if (registry == null)
            {
                EditorGUILayout.HelpBox($"{typeof(TRegistry).Name} 未找到", MessageType.Warning);
                return;
            }

            DrawToolbar(registry);
            DrawHeader(); // 固定表头在 ScrollView 外面

            scrollPos = GUILayout.BeginScrollView(scrollPos, GUIStyle.none, GUI.skin.verticalScrollbar);
            DrawContent(registry);
            GUILayout.EndScrollView();

            HandleDragging();
        }

        protected void DrawToolbar(TRegistry registry)
        {
            var enabledCount = registry.entries.Count(e => e.enabled);
            var disabledCount = registry.entries.Count - enabledCount;

            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            // 统计信息
            GUILayout.Label($"总计:{registry.entries.Count}", GUILayout.Width(55));
            GUILayout.Label($"启用:{enabledCount}", GUILayout.Width(50));
            GUILayout.Label($"禁用:{disabledCount}", GUILayout.Width(50));

            GUILayout.Space(10);

            // 搜索
            GUILayout.Label("搜索:", GUILayout.Width(35));
            searchFilter = EditorGUILayout.TextField(searchFilter, EditorStyles.toolbarSearchField, GUILayout.Width(120));

            GUILayout.Space(5);

            showOnlyDisabled = GUILayout.Toggle(showOnlyDisabled, "仅禁用", EditorStyles.toolbarButton, GUILayout.Width(50));
            showOnlyEnabled = GUILayout.Toggle(showOnlyEnabled, "仅启用", EditorStyles.toolbarButton, GUILayout.Width(50));

            GUILayout.FlexibleSpace();

            // 全部启用/禁用
            if (GUILayout.Button("全部启用", EditorStyles.toolbarButton, GUILayout.Width(60)))
            {
                foreach (var e in registry.entries) e.enabled = true;
                EditorUtility.SetDirty(registry);
            }
            if (GUILayout.Button("全部禁用", EditorStyles.toolbarButton, GUILayout.Width(60)))
            {
                foreach (var e in registry.entries) e.enabled = false;
                EditorUtility.SetDirty(registry);
            }

            GUILayout.Space(5);

            if (GUILayout.Button("刷新", EditorStyles.toolbarButton, GUILayout.Width(40)))
            {
                RegistryUpdater.UpdateAllRegistries();
            }

            EditorGUILayout.EndHorizontal();
        }

        protected void DrawHeader()
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            for (int i = 0; i < columns.Count; i++)
            {
                var col = columns[i];
                DrawHeaderCell(col.name, col.width, col.sortable, i < columns.Count - 1 ? i : -1);
            }

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }

        private void DrawHeaderCell(string label, float width, bool sortable, int colIndex)
        {
            var style = sortColumn == label ? EditorStyles.boldLabel : EditorStyles.label;
            var displayLabel = label;

            if (sortable && sortColumn == label)
            {
                displayLabel += sortAscending ? " ▲" : " ▼";
            }

            var rect = GUILayoutUtility.GetRect(new GUIContent(displayLabel), style, GUILayout.Width(width));

            if (sortable && Event.current.type == EventType.MouseDown && rect.Contains(Event.current.mousePosition))
            {
                if (sortColumn == label)
                    sortAscending = !sortAscending;
                else
                {
                    sortColumn = label;
                    sortAscending = true;
                }
                Event.current.Use();
                Repaint();
            }

            GUI.Label(rect, displayLabel, style);

            if (colIndex >= 0)
            {
                var splitterRect = GUILayoutUtility.GetRect(SplitterWidth, 18f, GUILayout.Width(SplitterWidth));
                var lineRect = new Rect(splitterRect.x + 1, splitterRect.y + 2, 1, splitterRect.height - 4);
                EditorGUI.DrawRect(lineRect, new Color(0.3f, 0.3f, 0.3f));
                EditorGUIUtility.AddCursorRect(splitterRect, MouseCursor.ResizeHorizontal);

                if (Event.current.type == EventType.MouseDown && splitterRect.Contains(Event.current.mousePosition))
                {
                    _draggingCol = colIndex;
                    _dragStartX = Event.current.mousePosition.x;
                    _dragStartWidth = columns[colIndex].width;
                    Event.current.Use();
                }
            }
        }

        private void HandleDragging()
        {
            if (_draggingCol < 0) return;

            if (Event.current.type == EventType.MouseDrag)
            {
                var delta = Event.current.mousePosition.x - _dragStartX;
                var col = columns[_draggingCol];
                col.width = Mathf.Max(MinColWidth, _dragStartWidth + delta);
                columns[_draggingCol] = col;
                Repaint();
                Event.current.Use();
            }
            else if (Event.current.type == EventType.MouseUp)
            {
                _draggingCol = -1;
                SaveColumnWidths();
                Event.current.Use();
            }
        }

        private string GetColumnWidthKey(int index) => $"Azathrix.{GetType().Name}.ColWidth.{index}";

        private void SaveColumnWidths()
        {
            for (int i = 0; i < columns.Count; i++)
            {
                EditorPrefs.SetFloat(GetColumnWidthKey(i), columns[i].width);
            }
        }

        private void LoadColumnWidths()
        {
            for (int i = 0; i < columns.Count; i++)
            {
                var key = GetColumnWidthKey(i);
                if (EditorPrefs.HasKey(key))
                {
                    var col = columns[i];
                    col.width = EditorPrefs.GetFloat(key);
                    columns[i] = col;
                }
            }
        }

        protected Rect BeginRow(bool enabled, int index)
        {
            // 使用更深的颜色与 toolbar 区分
            var bgColor = index % 2 == 0
                ? new Color(0.18f, 0.18f, 0.18f)
                : new Color(0.22f, 0.22f, 0.22f);

            var rect = EditorGUILayout.BeginHorizontal();
            EditorGUI.DrawRect(rect, bgColor);
            GUILayout.Space(16);

            if (!enabled) GUI.color = Color.gray;
            return rect;
        }

        protected void EndRow()
        {
            GUI.color = Color.white;
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }

        protected bool DrawEnableToggle(TRegistry registry, TEntry entry, float width)
        {
            var newEnabled = EditorGUILayout.Toggle(entry.enabled, GUILayout.Width(width));
            if (newEnabled != entry.enabled)
            {
                entry.enabled = newEnabled;
                EditorUtility.SetDirty(registry);
            }
            return newEnabled;
        }

        protected void DrawGroupHeader<T>(string groupName, List<T> entries, Action setAllEnabled, Action setAllDisabled)
            where T : RegistryEntryBase
        {
            if (!groupFoldouts.ContainsKey(groupName))
                groupFoldouts[groupName] = true;

            var enabledCount = entries.Count(e => e.enabled);

            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            groupFoldouts[groupName] = EditorGUILayout.Foldout(groupFoldouts[groupName],
                $"{groupName} ({enabledCount}/{entries.Count})", true);

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("All", EditorStyles.toolbarButton, GUILayout.Width(30)))
                setAllEnabled();
            if (GUILayout.Button("None", EditorStyles.toolbarButton, GUILayout.Width(40)))
                setAllDisabled();

            EditorGUILayout.EndHorizontal();
        }

        protected Rect BeginGroupHeader(string key, string label, int enabledCount, int totalCount)
        {
            if (!groupFoldouts.ContainsKey(key))
                groupFoldouts[key] = true;

            var rect = EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            // 分组背景色 - 深色，hover 时稍亮
            var isHover = rect.Contains(Event.current.mousePosition);
            var bgColor = isHover ? new Color(0.28f, 0.28f, 0.28f) : new Color(0.15f, 0.15f, 0.15f);
            EditorGUI.DrawRect(rect, bgColor);

            if (Event.current.type == EventType.MouseMove)
                Repaint();

            // 整个区域可点击
            if (Event.current.type == EventType.MouseDown && rect.Contains(Event.current.mousePosition))
            {
                // 排除右侧按钮区域（约80px）
                if (Event.current.mousePosition.x < rect.xMax - 80)
                {
                    groupFoldouts[key] = !groupFoldouts[key];
                    Event.current.Use();
                }
            }

            groupFoldouts[key] = EditorGUILayout.Foldout(groupFoldouts[key],
                $"{label} ({enabledCount}/{totalCount})", true);

            return rect;
        }

        protected void EndGroupHeader(TRegistry registry, List<TEntry> entries)
        {
            GUILayout.FlexibleSpace();

            if (GUILayout.Button("All", EditorStyles.toolbarButton, GUILayout.Width(30)))
            {
                foreach (var e in entries) e.enabled = true;
                EditorUtility.SetDirty(registry);
            }
            if (GUILayout.Button("None", EditorStyles.toolbarButton, GUILayout.Width(40)))
            {
                foreach (var e in entries) e.enabled = false;
                EditorUtility.SetDirty(registry);
            }

            EditorGUILayout.EndHorizontal();
        }

        protected bool MatchesFilter(params string[] values)
        {
            if (string.IsNullOrEmpty(searchFilter))
                return true;

            var filter = searchFilter.ToLower();
            return values.Any(v => v != null && v.ToLower().Contains(filter));
        }

        protected bool PassesEnabledFilter(bool enabled)
        {
            if (showOnlyDisabled && enabled) return false;
            if (showOnlyEnabled && !enabled) return false;
            return true;
        }

        protected IEnumerable<T> ApplySort<T>(IEnumerable<T> items, Func<T, string> getColumn, string columnName)
        {
            if (sortColumn != columnName) return items;
            return sortAscending ? items.OrderBy(getColumn) : items.OrderByDescending(getColumn);
        }
    }
}
