using System;
using System.Collections.Generic;
using System.Linq;
using Azathrix.Framework.Core.Launcher;
using Azathrix.Framework.Core.Pipeline;
using Azathrix.Framework.Tools;
using Cysharp.Threading.Tasks;

namespace Azathrix.Framework.Editor.Launcher
{
    /// <summary>
    /// 编辑器启动管线
    /// </summary>
    [PipelineId("EditorLauncher")]
    [PipelineDisplayName("编辑器启动")]
    public class EditorLauncherPipeline : PipelineBase<IEditorLauncherPhase, IEditorLauncherHook, LauncherContext>
    {
        /// <summary>
        /// 从注册表刷新阶段和钩子
        /// </summary>
        public void Refresh()
        {
            _phases.Clear();
            _beforeHooks.Clear();
            _afterHooks.Clear();

            var registry = PipelineRegistry.Instance;
            if (registry == null)
            {
                Log.Error("[EditorLauncher] PipelineRegistry 未找到");
                return;
            }

            // 加载阶段
            var phases = registry.GetOrderedPhases(Id);
            foreach (var entry in phases)
            {
                var type = entry.GetRuntimeType();
                if (type == null) continue;

                if (Activator.CreateInstance(type) is IEditorLauncherPhase phase)
                    _phases.Add(phase);
            }

            // 加载钩子
            var pipeline = registry.GetPipeline(Id);
            if (pipeline == null) return;

            foreach (var entry in pipeline.hooks.Where(h => h.enabled))
            {
                var hookType = entry.GetRuntimeType();
                if (hookType == null) continue;

                var phaseType = entry.GetTargetPhaseType();
                if (phaseType == null) continue;

                try
                {
                    var hook = Activator.CreateInstance(hookType);
                    var targetDict = entry.isBefore ? _beforeHooks : _afterHooks;

                    if (!targetDict.TryGetValue(phaseType, out var list))
                        targetDict[phaseType] = list = new List<object>();
                    list.Add(hook);
                }
                catch (Exception e)
                {
                    Log.Warning($"[EditorLauncher] 创建钩子 {entry.typeName} 失败: {e.Message}");
                }
            }

            // 排序钩子
            foreach (var key in _beforeHooks.Keys.ToList())
                _beforeHooks[key] = _beforeHooks[key].OrderBy(h => ((dynamic)h).Order).ToList();
            foreach (var key in _afterHooks.Keys.ToList())
                _afterHooks[key] = _afterHooks[key].OrderBy(h => ((dynamic)h).Order).ToList();
        }

        public override async UniTask ExecuteAsync(LauncherContext context)
        {
            if (_phases.Count == 0)
                Refresh();

            context.IsEditor = true;
            await base.ExecuteAsync(context);
        }
    }
}
