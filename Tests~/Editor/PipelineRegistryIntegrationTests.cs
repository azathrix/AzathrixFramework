using System.Collections.Generic;
using System.Threading.Tasks;
using Azathrix.Framework.Core.Pipeline;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;

namespace Azathrix.Framework.Tests
{
    public class PipelineRegistryIntegrationTests
    {
        private PipelineRegistry _registry;

        [SetUp]
        public void SetUp()
        {
            _registry = ScriptableObject.CreateInstance<PipelineRegistry>();
            PipelineRegistry.SetSettings(_registry);
            PipelineFactory.Refresh();
        }

        [TearDown]
        public void TearDown()
        {
            PipelineRegistry.SetSettings(null);
            if (_registry != null)
                Object.DestroyImmediate(_registry);
            RegistryTestLog.Log = null;
            PipelineFactory.Refresh();
        }

        [PipelineId("RegistryPipeline")]
        private class RegistryPipeline : PipelineBase<IRegistryPhase, RegistryContext> { }

        private class RegistryContext : PipelineContext { }

        private interface IRegistryPhase : IPhase<RegistryContext> { }

        private static class RegistryTestLog
        {
            public static List<string> Log;
        }

        [PhaseId("Registry")]
        private class RegistryPhase : IRegistryPhase
        {
            public int Order => 100;

            public UniTask ExecuteAsync(RegistryContext context)
            {
                RegistryTestLog.Log?.Add("Phase:Registry");
                return UniTask.CompletedTask;
            }
        }

        [HookTarget("RegistryPipeline", "Registry")]
        private class RegistryGlobalHook : IHook<RegistryContext>
        {
            public int Order => 0;

            public UniTask<HookResult> OnBeforeAsync(RegistryContext context)
            {
                RegistryTestLog.Log?.Add("Before:Global");
                return UniTask.FromResult(HookResult.Continue);
            }

            public UniTask OnAfterAsync(RegistryContext context)
            {
                RegistryTestLog.Log?.Add("After:Global");
                return UniTask.CompletedTask;
            }
        }

        private interface IRegistryTaggedPhase : IRegistryPhase { }

        private class RegistryBasePhase : IRegistryPhase
        {
            public int Order => 100;

            public virtual UniTask ExecuteAsync(RegistryContext context)
            {
                RegistryTestLog.Log?.Add("Phase:Base");
                return UniTask.CompletedTask;
            }
        }

        private class RegistryDerivedPhase : RegistryBasePhase, IRegistryTaggedPhase
        {
            public override UniTask ExecuteAsync(RegistryContext context)
            {
                RegistryTestLog.Log?.Add("Phase:Derived");
                return UniTask.CompletedTask;
            }
        }

        [HookTarget("RegistryPipeline", nameof(RegistryDerivedPhase))]
        private class RegistryBaseBeforeHook : IBeforePhaseHook<RegistryContext>
        {
            public int Order => 10;

            public UniTask<HookResult> OnBeforeAsync(RegistryContext context)
            {
                RegistryTestLog.Log?.Add("Before:BaseClass");
                return UniTask.FromResult(HookResult.Continue);
            }
        }

        [HookTarget("RegistryPipeline", nameof(RegistryDerivedPhase))]
        private class RegistryInterfaceBeforeHook : IBeforePhaseHook<RegistryContext>
        {
            public int Order => 20;

            public UniTask<HookResult> OnBeforeAsync(RegistryContext context)
            {
                RegistryTestLog.Log?.Add("Before:Interface");
                return UniTask.FromResult(HookResult.Continue);
            }
        }

        [HookTarget("RegistryPipeline", nameof(RegistryDerivedPhase))]
        private class RegistryConcreteBeforeHook : IBeforePhaseHook<RegistryContext>
        {
            public int Order => 30;

            public UniTask<HookResult> OnBeforeAsync(RegistryContext context)
            {
                RegistryTestLog.Log?.Add("Before:Concrete");
                return UniTask.FromResult(HookResult.Continue);
            }
        }

        [HookTarget("RegistryPipeline", "PhaseA")]
        [HookTarget("RegistryPipeline", "PhaseB")]
        private class RegistryMultiTargetHook : IBeforePhaseHook<RegistryContext>
        {
            public int Order => 0;

            public UniTask<HookResult> OnBeforeAsync(RegistryContext context)
            {
                RegistryTestLog.Log?.Add("Before:Multi");
                return UniTask.FromResult(HookResult.Continue);
            }
        }

        [PhaseId("PhaseA")]
        private class PhaseA : IRegistryPhase
        {
            public int Order => 100;

            public UniTask ExecuteAsync(RegistryContext context)
            {
                RegistryTestLog.Log?.Add("Phase:A");
                return UniTask.CompletedTask;
            }
        }

        [PhaseId("PhaseB")]
        private class PhaseB : IRegistryPhase
        {
            public int Order => 200;

            public UniTask ExecuteAsync(RegistryContext context)
            {
                RegistryTestLog.Log?.Add("Phase:B");
                return UniTask.CompletedTask;
            }
        }

        [Test]
        public async Task Registry_GlobalHook_Executes()
        {
            RegistryTestLog.Log = new List<string>();

            var pipeline = _registry.GetOrCreatePipeline("RegistryPipeline");
            pipeline.pipelineTypeName = typeof(RegistryPipeline).FullName;
            pipeline.pipelineAssembly = typeof(RegistryPipeline).Assembly.GetName().Name;
            pipeline.phases.Add(new PhaseEntry
            {
                typeName = typeof(RegistryPhase).FullName,
                assemblyName = typeof(RegistryPhase).Assembly.GetName().Name,
                order = 100,
                enabled = true,
                phaseId = "Registry"
            });
            pipeline.hooks.Add(new HookEntry
            {
                typeName = typeof(RegistryGlobalHook).FullName,
                assemblyName = typeof(RegistryGlobalHook).Assembly.GetName().Name,
                order = 0,
                enabled = true,
                isBefore = true,
                targets = new List<HookTargetEntry> { new HookTargetEntry { phaseId = "Registry" } }
            });
            pipeline.hooks.Add(new HookEntry
            {
                typeName = typeof(RegistryGlobalHook).FullName,
                assemblyName = typeof(RegistryGlobalHook).Assembly.GetName().Name,
                order = 0,
                enabled = true,
                isBefore = false,
                targets = new List<HookTargetEntry> { new HookTargetEntry { phaseId = "Registry" } }
            });

            var runtime = PipelineFactory.Get("RegistryPipeline") as RegistryPipeline;
            await runtime.ExecuteAsync(new RegistryContext());

            Assert.AreEqual(new[] { "Before:Global", "Phase:Registry", "After:Global" }, RegistryTestLog.Log.ToArray());
        }

        [Test]
        public void PipelineFactory_ReturnsCachedInstance()
        {
            var pipeline = _registry.GetOrCreatePipeline("RegistryPipeline");
            pipeline.pipelineTypeName = typeof(RegistryPipeline).FullName;
            pipeline.pipelineAssembly = typeof(RegistryPipeline).Assembly.GetName().Name;

            var first = PipelineFactory.Get("RegistryPipeline");
            var second = PipelineFactory.Get("RegistryPipeline");

            Assert.AreSame(first, second);
        }

        [Test]
        public async Task Registry_TypeHooks_Match_SamePhaseId()
        {
            RegistryTestLog.Log = new List<string>();

            var pipeline = _registry.GetOrCreatePipeline("RegistryPipeline");
            pipeline.pipelineTypeName = typeof(RegistryPipeline).FullName;
            pipeline.pipelineAssembly = typeof(RegistryPipeline).Assembly.GetName().Name;
            pipeline.phases.Add(new PhaseEntry
            {
                typeName = typeof(RegistryDerivedPhase).FullName,
                assemblyName = typeof(RegistryDerivedPhase).Assembly.GetName().Name,
                order = 100,
                enabled = true
            });
            pipeline.hooks.Add(new HookEntry
            {
                typeName = typeof(RegistryBaseBeforeHook).FullName,
                assemblyName = typeof(RegistryBaseBeforeHook).Assembly.GetName().Name,
                order = 10,
                enabled = true,
                isBefore = true,
                targets = new List<HookTargetEntry> { new HookTargetEntry { phaseId = nameof(RegistryDerivedPhase) } }
            });
            pipeline.hooks.Add(new HookEntry
            {
                typeName = typeof(RegistryInterfaceBeforeHook).FullName,
                assemblyName = typeof(RegistryInterfaceBeforeHook).Assembly.GetName().Name,
                order = 20,
                enabled = true,
                isBefore = true,
                targets = new List<HookTargetEntry> { new HookTargetEntry { phaseId = nameof(RegistryDerivedPhase) } }
            });
            pipeline.hooks.Add(new HookEntry
            {
                typeName = typeof(RegistryConcreteBeforeHook).FullName,
                assemblyName = typeof(RegistryConcreteBeforeHook).Assembly.GetName().Name,
                order = 30,
                enabled = true,
                isBefore = true,
                targets = new List<HookTargetEntry> { new HookTargetEntry { phaseId = nameof(RegistryDerivedPhase) } }
            });

            var runtime = PipelineFactory.Get("RegistryPipeline") as RegistryPipeline;
            await runtime.ExecuteAsync(new RegistryContext());

            var phaseIndex = RegistryTestLog.Log.IndexOf("Phase:Derived");
            Assert.IsTrue(phaseIndex >= 0);
            Assert.Less(RegistryTestLog.Log.IndexOf("Before:BaseClass"), phaseIndex);
            Assert.Less(RegistryTestLog.Log.IndexOf("Before:Interface"), phaseIndex);
            Assert.Less(RegistryTestLog.Log.IndexOf("Before:Concrete"), phaseIndex);
        }

        [Test]
        public async Task Registry_HookTargets_MultiplePhases()
        {
            RegistryTestLog.Log = new List<string>();

            var pipeline = _registry.GetOrCreatePipeline("RegistryPipeline");
            pipeline.pipelineTypeName = typeof(RegistryPipeline).FullName;
            pipeline.pipelineAssembly = typeof(RegistryPipeline).Assembly.GetName().Name;
            pipeline.phases.Add(new PhaseEntry
            {
                typeName = typeof(PhaseA).FullName,
                assemblyName = typeof(PhaseA).Assembly.GetName().Name,
                order = 100,
                enabled = true,
                phaseId = "PhaseA"
            });
            pipeline.phases.Add(new PhaseEntry
            {
                typeName = typeof(PhaseB).FullName,
                assemblyName = typeof(PhaseB).Assembly.GetName().Name,
                order = 200,
                enabled = true,
                phaseId = "PhaseB"
            });
            pipeline.hooks.Add(new HookEntry
            {
                typeName = typeof(RegistryMultiTargetHook).FullName,
                assemblyName = typeof(RegistryMultiTargetHook).Assembly.GetName().Name,
                order = 0,
                enabled = true,
                isBefore = true,
                targets = new List<HookTargetEntry>
                {
                    new HookTargetEntry { phaseId = "PhaseA" },
                    new HookTargetEntry { phaseId = "PhaseB" }
                }
            });

            var runtime = PipelineFactory.Get("RegistryPipeline") as RegistryPipeline;
            await runtime.ExecuteAsync(new RegistryContext());

            Assert.AreEqual(new[] { "Before:Multi", "Phase:A", "Before:Multi", "Phase:B" }, RegistryTestLog.Log.ToArray());
        }
    }
}
