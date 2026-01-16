using System.Collections.Generic;
using System.Threading.Tasks;
using Azathrix.Framework.Core.Launcher;
using Azathrix.Framework.Core.Pipeline;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Azathrix.Framework.Samples.Pipeline
{
    internal static class SamplePipelineRuntime
    {
        internal static bool AutoRunOnStartup = true;
        internal static bool ShouldRun;
        private static readonly List<string> Log = new();

        internal static void RequestRun()
        {
            ShouldRun = true;
            Log.Clear();
        }

        internal static void Add(string message)
        {
            Log.Add(message);
        }

        internal static void FlushLogs()
        {
            foreach (var entry in Log)
                Debug.Log($"[PipelineSample] {entry}");
        }

        internal static async UniTask RunAsync()
        {
            var pipeline = PipelineFactory.Get("SamplePipeline") as SamplePipeline;
            if (pipeline == null)
            {
                Debug.LogWarning("[PipelineSample] SamplePipeline 未找到");
                return;
            }

            pipeline.SilentMode = true;
            await pipeline.ExecuteAsync(new SampleContext());
            FlushLogs();
        }
    }

    [PipelineId("SamplePipeline")]
    [PipelineDisplayName("Pipeline Sample")]
    [Register]
    internal class SamplePipeline : PipelineBase<ISamplePhase, SampleContext>
    {
    }

    internal class SampleContext : PipelineContext { }

    internal interface ISamplePhase : IPhase<SampleContext> { }

    [PipelineId("SamplePipeline")]
    [PhaseId("Sample")]
    [Register]
    internal class SamplePhase : ISamplePhase
    {
        public int Order => 100;

        public UniTask ExecuteAsync(SampleContext context)
        {
            SamplePipelineRuntime.Add("Phase:Sample");
            return UniTask.CompletedTask;
        }
    }

    [Register]
    [HookTarget("SamplePipeline", "Sample")]
    internal class SampleBeforeHook : IBeforePhaseHook<SampleContext>
    {
        public int Order => 0;

        public UniTask<HookResult> OnBeforeAsync(SampleContext context)
        {
            SamplePipelineRuntime.Add("Before:Sample");
            return UniTask.FromResult(HookResult.Continue);
        }
    }

    [Register]
    [HookTarget("SamplePipeline", "Sample")]
    internal class SampleAfterHook : IAfterPhaseHook<SampleContext>
    {
        public int Order => 0;

        public UniTask OnAfterAsync(SampleContext context)
        {
            SamplePipelineRuntime.Add("After:Sample");
            return UniTask.CompletedTask;
        }
    }

    [Register]
    [HookTarget("Launcher", "Start")]
    internal class SamplePipelineTriggerHook : IAfterPhaseHook<LauncherContext>
    {
        public int Order => 0;

        public async UniTask OnAfterAsync(LauncherContext context)
        {
            if (!SamplePipelineRuntime.AutoRunOnStartup && !SamplePipelineRuntime.ShouldRun)
                return;

            SamplePipelineRuntime.ShouldRun = false;
            await SamplePipelineRuntime.RunAsync();
        }
    }
}
