using System.Collections.Generic;
using System.Linq;
using Azathrix.Framework.Registry;
using UnityEditor;
using UnityEngine;

namespace Azathrix.Framework.Editor.Registry
{
    public class PhaseRegistryWindow : RegistryWindowBase<PhaseRegistry, PhaseEntry>
    {
        protected override string WindowTitle => "阶段注册表";

        [MenuItem("Azathrix/注册表/阶段注册表")]
        public static void ShowWindow()
        {
            var window = GetWindow<PhaseRegistryWindow>("阶段注册表");
            window.minSize = new Vector2(550, 400);
        }

        protected override void OnEnable()
        {
            columns = new List<ColumnDef>
            {
                new() { name = "启用", width = 50, sortable = true },
                new() { name = "顺序", width = 60, sortable = true },
                new() { name = "名称", width = 200, sortable = true },
                new() { name = "编辑器支持", width = 80, sortable = true },
            };
            base.OnEnable();
        }

        protected override PhaseRegistry GetRegistry() => PhaseRegistry.Instance;

        protected override void DrawContent(PhaseRegistry registry)
        {
            // 运行时阶段
            var runtimePhases = registry.entries.Where(e => !e.editorOnly).ToList();
            var editorOnlyPhases = registry.entries.Where(e => e.editorOnly).ToList();

            DrawPhaseGroup(registry, "运行时阶段", runtimePhases);

            if (editorOnlyPhases.Count > 0)
            {
                DrawPhaseGroup(registry, "仅编辑器阶段", editorOnlyPhases);
            }
        }

        private void DrawPhaseGroup(PhaseRegistry registry, string groupName, List<PhaseEntry> entries)
        {
            var key = $"phase_{groupName}";
            var enabledCount = entries.Count(e => e.enabled);

            // 分组头
            BeginGroupHeader(key, groupName, enabledCount, entries.Count);

            GUILayout.FlexibleSpace();

            GUI.contentColor = new Color(0.8f, 0.8f, 0.8f);
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
            GUI.contentColor = Color.white;

            EditorGUILayout.EndHorizontal();

            if (!groupFoldouts[key]) return;

            // 过滤和排序
            var filtered = entries
                .Where(e => MatchesFilter(e.displayName, e.typeName))
                .Where(e => PassesEnabledFilter(e.enabled))
                .ToList();

            filtered = ApplySorting(filtered);

            for (int i = 0; i < filtered.Count; i++)
            {
                DrawRow(registry, filtered[i], i);
            }
        }

        private List<PhaseEntry> ApplySorting(List<PhaseEntry> entries)
        {
            if (string.IsNullOrEmpty(sortColumn))
                return entries.OrderBy(e => e.order).ToList();

            IEnumerable<PhaseEntry> sorted = sortColumn switch
            {
                "启用" => sortAscending ? entries.OrderBy(e => e.enabled) : entries.OrderByDescending(e => e.enabled),
                "顺序" => sortAscending ? entries.OrderBy(e => e.order) : entries.OrderByDescending(e => e.order),
                "名称" => sortAscending ? entries.OrderBy(e => e.displayName) : entries.OrderByDescending(e => e.displayName),
                "编辑器支持" => sortAscending ? entries.OrderBy(e => e.editorSupport) : entries.OrderByDescending(e => e.editorSupport),
                _ => entries.OrderBy(e => e.order)
            };

            return sorted.ToList();
        }

        private void DrawRow(PhaseRegistry registry, PhaseEntry entry, int index)
        {
            BeginRow(entry.enabled, index);

            // 启用
            DrawEnableToggle(registry, entry, columns[0].width + SplitterWidth);

            // 顺序
            EditorGUILayout.LabelField(entry.order.ToString(), GUILayout.Width(columns[1].width + SplitterWidth));

            // 名称
            EditorGUILayout.LabelField(entry.displayName, GUILayout.Width(columns[2].width + SplitterWidth));

            // 编辑器支持
            GUI.color = entry.editorSupport ? Color.green : Color.gray;
            EditorGUILayout.LabelField(entry.editorSupport ? "✓" : "-", GUILayout.Width(columns[3].width));

            EndRow();
        }
    }
}
