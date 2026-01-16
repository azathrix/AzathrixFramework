using System.Collections.Generic;
using System.Linq;
using Azathrix.Framework.Core.Pipeline;
using Azathrix.Framework.Editor.Pipeline;
using NUnit.Framework;
using UnityEngine;

namespace Azathrix.Framework.Tests
{
    public class PipelineRegistryScannerTests
    {
        private PipelineRegistry _registry;

        [SetUp]
        public void SetUp()
        {
            _registry = ScriptableObject.CreateInstance<PipelineRegistry>();
            PipelineRegistry.SetSettings(_registry);
        }

        [TearDown]
        public void TearDown()
        {
            PipelineRegistry.SetSettings(null);
            if (_registry != null)
                Object.DestroyImmediate(_registry);
        }

        [Register]
        [PipelineId("ScannerPipeline")]
        private class ScannerPipeline : PipelineBase<IScannerPhase, ScannerContext> { }

        [PipelineId("UnregisteredPipeline")]
        private class UnregisteredPipeline : PipelineBase<IScannerPhase, ScannerContext> { }

        private class ScannerContext : PipelineContext { }

        private interface IScannerPhase : IPhase<ScannerContext> { }

        [Register]
        [PipelineId("ScannerPipeline")]
        [PhaseId("ScanPhase")]
        private class ScanPhase : IScannerPhase
        {
            public int Order => 100;

            public Cysharp.Threading.Tasks.UniTask ExecuteAsync(ScannerContext context)
            {
                return Cysharp.Threading.Tasks.UniTask.CompletedTask;
            }
        }

        [PipelineId("ScannerPipeline")]
        [PhaseId("IgnoredPhase")]
        private class IgnoredPhase : IScannerPhase
        {
            public int Order => 200;

            public Cysharp.Threading.Tasks.UniTask ExecuteAsync(ScannerContext context)
            {
                return Cysharp.Threading.Tasks.UniTask.CompletedTask;
            }
        }

        [PipelineId("ScannerPipeline")]
        [PhaseId("UnregisteredPhase")]
        private class UnregisteredPhase : IScannerPhase
        {
            public int Order => 300;

            public Cysharp.Threading.Tasks.UniTask ExecuteAsync(ScannerContext context)
            {
                return Cysharp.Threading.Tasks.UniTask.CompletedTask;
            }
        }

        [Register]
        [HookTarget("ScannerPipeline", "ScanPhase")]
        private class ScanBeforeHook : IBeforePhaseHook<ScannerContext>
        {
            public int Order => 0;

            public Cysharp.Threading.Tasks.UniTask<HookResult> OnBeforeAsync(ScannerContext context)
            {
                return Cysharp.Threading.Tasks.UniTask.FromResult(HookResult.Continue);
            }
        }

        [Register]
        [HookTarget("ScannerPipeline", "MissingPhase")]
        private class OrphanHook : IBeforePhaseHook<ScannerContext>
        {
            public int Order => 0;

            public Cysharp.Threading.Tasks.UniTask<HookResult> OnBeforeAsync(ScannerContext context)
            {
                return Cysharp.Threading.Tasks.UniTask.FromResult(HookResult.Continue);
            }
        }

        [Register]
        [HookTarget("ScannerPipeline", "")]
        private class InvalidTargetHook : IBeforePhaseHook<ScannerContext>
        {
            public int Order => 0;

            public Cysharp.Threading.Tasks.UniTask<HookResult> OnBeforeAsync(ScannerContext context)
            {
                return Cysharp.Threading.Tasks.UniTask.FromResult(HookResult.Continue);
            }
        }

        [HookTarget("ScannerPipeline", "ScanPhase")]
        private class UnregisteredHook : IBeforePhaseHook<ScannerContext>
        {
            public int Order => 0;

            public Cysharp.Threading.Tasks.UniTask<HookResult> OnBeforeAsync(ScannerContext context)
            {
                return Cysharp.Threading.Tasks.UniTask.FromResult(HookResult.Continue);
            }
        }

        [Test]
        public void Scanner_RespectsRegisterAndCleansInvalidEntries()
        {
            var pipeline = _registry.GetOrCreatePipeline("ScannerPipeline");
            var unregisteredPipeline = _registry.GetOrCreatePipeline("UnregisteredPipeline");
            unregisteredPipeline.pipelineTypeName = typeof(UnregisteredPipeline).FullName;
            unregisteredPipeline.pipelineAssembly = typeof(UnregisteredPipeline).Assembly.GetName().Name;
            unregisteredPipeline.phases.Add(new PhaseEntry
            {
                typeName = typeof(ScanPhase).FullName,
                assemblyName = typeof(ScanPhase).Assembly.GetName().Name,
                phaseId = "ScanPhase",
                enabled = true
            });
            pipeline.hooks.Add(new HookEntry
            {
                typeName = typeof(ScanBeforeHook).FullName,
                assemblyName = typeof(ScanBeforeHook).Assembly.GetName().Name,
                enabled = true,
                isBefore = true,
                targets = new List<HookTargetEntry>()
            });
            pipeline.phases.Add(new PhaseEntry
            {
                typeName = typeof(UnregisteredPhase).FullName,
                assemblyName = typeof(UnregisteredPhase).Assembly.GetName().Name,
                phaseId = "UnregisteredPhase",
                enabled = true
            });
            pipeline.hooks.Add(new HookEntry
            {
                typeName = typeof(UnregisteredHook).FullName,
                assemblyName = typeof(UnregisteredHook).Assembly.GetName().Name,
                enabled = true,
                isBefore = true,
                targets = new List<HookTargetEntry> { new HookTargetEntry { phaseId = "ScanPhase" } }
            });
            pipeline.hooks.Add(new HookEntry
            {
                typeName = "Dummy.Hook",
                assemblyName = "Dummy.Assembly",
                enabled = true,
                isBefore = true,
                targets = new List<HookTargetEntry>
                {
                    new HookTargetEntry { phaseId = "ScanPhase" },
                    new HookTargetEntry { phaseId = "OtherPhase" }
                }
            });

            PipelineRegistryScanner.ScanAll();

            pipeline = _registry.GetPipeline("ScannerPipeline");
            Assert.NotNull(pipeline);
            Assert.IsNull(_registry.GetPipeline("UnregisteredPipeline"));

            Assert.IsTrue(pipeline.phases.Any(p => p.typeName == typeof(ScanPhase).FullName));
            Assert.IsFalse(pipeline.phases.Any(p => p.typeName == typeof(IgnoredPhase).FullName));
            Assert.IsFalse(pipeline.phases.Any(p => p.typeName == typeof(UnregisteredPhase).FullName));

            Assert.IsTrue(pipeline.hooks.Any(h =>
                h.typeName == typeof(ScanBeforeHook).FullName &&
                h.targets.Count == 1 &&
                h.targets[0].phaseId == "ScanPhase"));

            Assert.IsFalse(pipeline.hooks.Any(h => h.typeName == typeof(OrphanHook).FullName));
            Assert.IsFalse(pipeline.hooks.Any(h => h.typeName == typeof(UnregisteredHook).FullName));
            Assert.IsFalse(pipeline.hooks.Any(h => h.typeName == typeof(InvalidTargetHook).FullName));

            Assert.IsFalse(pipeline.hooks.Any(h =>
                h.targets == null ||
                h.targets.Count == 0 ||
                h.targets.Any(t => string.IsNullOrEmpty(t.phaseId)) ||
                h.targets.Count > 1));
        }
    }
}
