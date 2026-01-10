using System.Collections.Generic;
using System.Linq;
using Azathrix.Framework.Registry;
using UnityEditor;
using UnityEngine;

namespace Azathrix.Framework.Editor.Registry
{
    public class StartupHookRegistryWindow : RegistryWindowBase<StartupHookRegistry, StartupHookEntry>
    {
        protected override string WindowTitle => "启动钩子注册表";

        [MenuItem("Azathrix/注册表/启动钩子注册表")]
        public static void ShowWindow()
        {
            var window = GetWindow<StartupHookRegistryWindow>("启动钩子注册表");
            window.minSize = new Vector2(550, 400);
        }

        protected override void OnEnable()
        {
            columns = new List<ColumnDef>
            {
                new() { name = "启用", width = 50, sortable = true },
                new() { name = "顺序", width = 60, sortable = true },
                new() { name = "名称", width = 200, sortable = true },
                new() { name = "类型", width = 60, sortable = true },
                new() { name = "目标阶段", width = 150, sortable = true },
            };
            base.OnEnable();
        }

        protected override StartupHookRegistry GetRegistry() => StartupHookRegistry.Instance;

        protected override void DrawContent(StartupHookRegistry registry)
        {
            // 按目标阶段分组
            var groups = registry.entries
                .Where(e => MatchesFilter(e.displayName, e.typeName, e.targetPhaseType))
                .Where(e => PassesEnabledFilter(e.enabled))
                .GroupBy(e => e.targetPhaseType ?? "Unknown")
                .OrderBy(g => g.Key);

            foreach (var group in groups)
            {
                DrawGroup(registry, group.Key.Split('.').Last(), group.ToList());
            }
        }

        private void DrawGroup(StartupHookRegistry registry, string groupName, List<StartupHookEntry> entries)
        {
            var key = $"hook_{groupName}";
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

            // 条目列表
            var sorted = ApplySorting(entries);

            for (int i = 0; i < sorted.Count; i++)
            {
                DrawRow(registry, sorted[i], i);
            }
        }

        private List<StartupHookEntry> ApplySorting(List<StartupHookEntry> entries)
        {
            if (string.IsNullOrEmpty(sortColumn))
                return entries.OrderBy(e => e.isBefore ? 0 : 1).ThenBy(e => e.order).ToList();

            IEnumerable<StartupHookEntry> sorted = sortColumn switch
            {
                "启用" => sortAscending ? entries.OrderBy(e => e.enabled) : entries.OrderByDescending(e => e.enabled),
                "顺序" => sortAscending ? entries.OrderBy(e => e.order) : entries.OrderByDescending(e => e.order),
                "名称" => sortAscending ? entries.OrderBy(e => e.displayName) : entries.OrderByDescending(e => e.displayName),
                "类型" => sortAscending ? entries.OrderBy(e => e.isBefore) : entries.OrderByDescending(e => e.isBefore),
                "目标阶段" => sortAscending ? entries.OrderBy(e => e.targetPhaseType) : entries.OrderByDescending(e => e.targetPhaseType),
                _ => entries.OrderBy(e => e.isBefore ? 0 : 1).ThenBy(e => e.order)
            };

            return sorted.ToList();
        }

        private void DrawRow(StartupHookRegistry registry, StartupHookEntry entry, int index)
        {
            BeginRow(entry.enabled, index);

            // 启用
            DrawEnableToggle(registry, entry, columns[0].width + SplitterWidth);

            // 顺序
            EditorGUILayout.LabelField(entry.order.ToString(), GUILayout.Width(columns[1].width + SplitterWidth));

            // 名称
            EditorGUILayout.LabelField(entry.displayName, GUILayout.Width(columns[2].width + SplitterWidth));

            // 类型
            GUI.color = entry.isBefore ? new Color(0.5f, 0.8f, 1f) : new Color(1f, 0.8f, 0.5f);
            EditorGUILayout.LabelField(entry.isBefore ? "前置" : "后置", GUILayout.Width(columns[3].width + SplitterWidth));

            // 目标阶段
            GUI.color = entry.enabled ? Color.white : Color.gray;
            var phaseName = entry.targetPhaseType?.Split('.').Last() ?? "-";
            EditorGUILayout.LabelField(phaseName, GUILayout.Width(columns[4].width));

            EndRow();
        }
    }
}
