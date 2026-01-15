using System.Collections.Generic;
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
    internal class SamplePipeline : PipelineBase<ISamplePhase, SampleContext>
    {
    }

    internal class SampleContext : PipelineContext { }

    internal interface ISamplePhase : IPhase<SampleContext> { }

    internal interface ISampleTaggedPhase : ISamplePhase { }

    [PipelineId("SamplePipeline")]
    [PhaseId("Base")]
    internal abstract class SampleBasePhase : ISamplePhase
    {
        public int Order => 100;

        public virtual UniTask ExecuteAsync(SampleContext context)
        {
            SamplePipelineRuntime.Add("Phase:Base");
            return UniTask.CompletedTask;
        }
    }

    [PipelineId("SamplePipeline")]
    [PhaseId("Derived")]
    internal class SampleDerivedPhase : SampleBasePhase, ISampleTaggedPhase
    {
        public override UniTask ExecuteAsync(SampleContext context)
        {
            SamplePipelineRuntime.Add("Phase:Derived");
            return UniTask.CompletedTask;
        }
    }

    [HookTarget("SamplePipeline", "Base")]
    [HookTarget("SamplePipeline", "Derived")]
    internal class SampleGlobalHook : IHook<SampleContext>
    {
        public int Order => 5;

        public UniTask<HookResult> OnBeforeAsync(SampleContext context)
        {
            SamplePipelineRuntime.Add("Before:Global");
            return UniTask.FromResult(HookResult.Continue);
        }

        public UniTask OnAfterAsync(SampleContext context)
        {
            SamplePipelineRuntime.Add("After:Global");
            return UniTask.CompletedTask;
        }
    }

    [HookTarget("SamplePipeline", nameof(SampleBasePhase))]
    internal class SampleBaseBeforeHook : IBeforePhaseHook<SampleContext>
    {
        public int Order => 10;

        public UniTask<HookResult> OnBeforeAsync(SampleContext context)
        {
            SamplePipelineRuntime.Add("Before:BaseClass");
            return UniTask.FromResult(HookResult.Continue);
        }
    }

    [HookTarget("SamplePipeline", "Derived")]
    internal class SampleInterfaceBeforeHook : IBeforePhaseHook<SampleContext>
    {
        public int Order => 20;

        public UniTask<HookResult> OnBeforeAsync(SampleContext context)
        {
            SamplePipelineRuntime.Add("Before:Interface");
            return UniTask.FromResult(HookResult.Continue);
        }
    }

    [HookTarget("SamplePipeline", "Derived")]
    internal class SampleConcreteBeforeHook : IBeforePhaseHook<SampleContext>
    {
        public int Order => 30;

        public UniTask<HookResult> OnBeforeAsync(SampleContext context)
        {
            SamplePipelineRuntime.Add("Before:Concrete");
            return UniTask.FromResult(HookResult.Continue);
        }
    }

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
