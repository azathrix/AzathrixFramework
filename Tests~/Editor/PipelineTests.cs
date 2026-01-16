using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Azathrix.Framework.Core.Pipeline;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Azathrix.Framework.Tests
{
    public class PipelineTests
    {
        private class TestContext : PipelineContext { }

        private interface ITestPhase : IPhase<TestContext> { }

        private class TestPipeline : PipelineBase<ITestPhase, TestContext> { }

        private class RecordingPhase : ITestPhase
        {
            public int Order { get; }
            private readonly string _label;
            private readonly List<string> _log;

            public RecordingPhase(string label, int order, List<string> log)
            {
                _label = label;
                Order = order;
                _log = log;
            }

            public UniTask ExecuteAsync(TestContext context)
            {
                _log.Add($"Phase:{_label}");
                return UniTask.CompletedTask;
            }
        }

        [PhaseId("A")]
        private class PhaseA : ITestPhase
        {
            public int Order => 100;
            private readonly List<string> _log;

            public PhaseA(List<string> log) => _log = log;

            public UniTask ExecuteAsync(TestContext context)
            {
                _log.Add("Phase:A");
                return UniTask.CompletedTask;
            }
        }

        [PhaseId("B")]
        private class PhaseB : ITestPhase
        {
            public int Order => 200;
            private readonly List<string> _log;

            public PhaseB(List<string> log) => _log = log;

            public UniTask ExecuteAsync(TestContext context)
            {
                _log.Add("Phase:B");
                return UniTask.CompletedTask;
            }
        }

        private class ThrowingPhase : ITestPhase
        {
            public int Order { get; }

            public ThrowingPhase(int order) => Order = order;

            public UniTask ExecuteAsync(TestContext context)
            {
                throw new InvalidOperationException("Test exception");
            }
        }

        [HookTarget("TestPipeline", "A")]
        [HookTarget("TestPipeline", "B")]
        private class GlobalHook : IHook<TestContext>
        {
            public int Order => 0;
            private readonly List<string> _log;

            public GlobalHook(List<string> log) => _log = log;

            public UniTask<HookResult> OnBeforeAsync(TestContext context)
            {
                _log.Add("Before:Global");
                return UniTask.FromResult(HookResult.Continue);
            }

            public UniTask OnAfterAsync(TestContext context)
            {
                _log.Add("After:Global");
                return UniTask.CompletedTask;
            }
        }

        [HookTarget("TestPipeline", "B")]
        private class PhaseBHook : IHook<TestContext>
        {
            public int Order => 0;
            private readonly List<string> _log;

            public PhaseBHook(List<string> log) => _log = log;

            public UniTask<HookResult> OnBeforeAsync(TestContext context)
            {
                _log.Add("Before:B");
                return UniTask.FromResult(HookResult.Continue);
            }

            public UniTask OnAfterAsync(TestContext context)
            {
                _log.Add("After:B");
                return UniTask.CompletedTask;
            }
        }

        [HookTarget("TestPipeline", "B")]
        private class SkipPhaseHook : IBeforePhaseHook<TestContext>
        {
            public int Order => 0;
            private readonly List<string> _log;

            public SkipPhaseHook(List<string> log) => _log = log;

            public UniTask<HookResult> OnBeforeAsync(TestContext context)
            {
                _log.Add("Before:SkipPhase");
                return UniTask.FromResult(HookResult.SkipPhase);
            }
        }

        [HookTarget("TestPipeline", "B")]
        private class PhaseBAfterHook : IAfterPhaseHook<TestContext>
        {
            public int Order => 0;
            private readonly List<string> _log;

            public PhaseBAfterHook(List<string> log) => _log = log;

            public UniTask OnAfterAsync(TestContext context)
            {
                _log.Add("After:B");
                return UniTask.CompletedTask;
            }
        }

        [HookTarget("TestPipeline", "B")]
        private class AbortHook : IBeforePhaseHook<TestContext>
        {
            public int Order => 0;
            private readonly List<string> _log;

            public AbortHook(List<string> log) => _log = log;

            public UniTask<HookResult> OnBeforeAsync(TestContext context)
            {
                _log.Add("Before:Abort");
                return UniTask.FromResult(HookResult.Abort);
            }
        }

        [HookTarget("TestPipeline", "A")]
        private class NonGenericBeforeHook : IBeforePhaseHook
        {
            public int Order => 10;
            private readonly List<string> _log;

            public NonGenericBeforeHook(List<string> log) => _log = log;

            public UniTask<HookResult> OnBeforeAsync(PipelineContext context)
            {
                _log.Add("Before:NonGeneric");
                return UniTask.FromResult(HookResult.Continue);
            }
        }

        [HookTarget("TestPipeline", "A")]
        private class NonGenericAfterHook : IAfterPhaseHook
        {
            public int Order => 20;
            private readonly List<string> _log;

            public NonGenericAfterHook(List<string> log) => _log = log;

            public UniTask OnAfterAsync(PipelineContext context)
            {
                _log.Add("After:NonGeneric");
                return UniTask.CompletedTask;
            }
        }

        [HookTarget("TestPipeline", "A")]
        private class NonGenericHook : IHook
        {
            public int Order => 0;
            private readonly List<string> _log;

            public NonGenericHook(List<string> log) => _log = log;

            public UniTask<HookResult> OnBeforeAsync(PipelineContext context)
            {
                _log.Add("Before:NonGenericBoth");
                return UniTask.FromResult(HookResult.Continue);
            }

            public UniTask OnAfterAsync(PipelineContext context)
            {
                _log.Add("After:NonGenericBoth");
                return UniTask.CompletedTask;
            }
        }

        private class ManualIdHook : IBeforePhaseHook<TestContext>
        {
            public int Order { get; }
            private readonly List<string> _log;
            private readonly string _name;

            public ManualIdHook(int order, List<string> log, string name)
            {
                Order = order;
                _log = log;
                _name = name;
            }

            public UniTask<HookResult> OnBeforeAsync(TestContext context)
            {
                _log.Add($"Before:{_name}");
                return UniTask.FromResult(HookResult.Continue);
            }
        }

        private interface ISpecialPhase : ITestPhase { }

        private class BasePhase : ITestPhase
        {
            public int Order => 100;
            private readonly List<string> _log;

            public BasePhase(List<string> log) => _log = log;

            public virtual UniTask ExecuteAsync(TestContext context)
            {
                _log.Add("Phase:Base");
                return UniTask.CompletedTask;
            }
        }

        private class DerivedPhase : BasePhase, ISpecialPhase
        {
            public DerivedPhase(List<string> log) : base(log) { }

            public override UniTask ExecuteAsync(TestContext context)
            {
                base.ExecuteAsync(context);
                return UniTask.CompletedTask;
            }
        }

        [HookTarget("TestPipeline", nameof(DerivedPhase))]
        private class BaseClassBeforeHook : IBeforePhaseHook<TestContext>
        {
            public int Order { get; }
            private readonly List<string> _log;

            public BaseClassBeforeHook(int order, List<string> log)
            {
                Order = order;
                _log = log;
            }

            public UniTask<HookResult> OnBeforeAsync(TestContext context)
            {
                _log.Add("Before:BaseClass");
                return UniTask.FromResult(HookResult.Continue);
            }
        }

        [HookTarget("TestPipeline", nameof(DerivedPhase))]
        private class InterfaceBeforeHook : IBeforePhaseHook<TestContext>
        {
            public int Order { get; }
            private readonly List<string> _log;

            public InterfaceBeforeHook(int order, List<string> log)
            {
                Order = order;
                _log = log;
            }

            public UniTask<HookResult> OnBeforeAsync(TestContext context)
            {
                _log.Add("Before:Interface");
                return UniTask.FromResult(HookResult.Continue);
            }
        }

        [HookTarget("TestPipeline", nameof(DerivedPhase))]
        private class ConcreteBeforeHook : IBeforePhaseHook<TestContext>
        {
            public int Order { get; }
            private readonly List<string> _log;

            public ConcreteBeforeHook(int order, List<string> log)
            {
                Order = order;
                _log = log;
            }

            public UniTask<HookResult> OnBeforeAsync(TestContext context)
            {
                _log.Add("Before:Concrete");
                return UniTask.FromResult(HookResult.Continue);
            }
        }

        [HookTarget("TestPipeline", nameof(DerivedPhase))]
        private class SkipHooksTypeHook : IBeforePhaseHook<TestContext>
        {
            public int Order { get; }
            private readonly List<string> _log;

            public SkipHooksTypeHook(int order, List<string> log)
            {
                Order = order;
                _log = log;
            }

            public UniTask<HookResult> OnBeforeAsync(TestContext context)
            {
                _log.Add("Before:SkipHooks");
                return UniTask.FromResult(HookResult.SkipHooks);
            }
        }

        [HookTarget("TestPipeline", nameof(DerivedPhase))]
        private class OrderedTypeBeforeHook : IBeforePhaseHook<TestContext>
        {
            public int Order { get; }
            private readonly List<string> _log;
            private readonly string _name;

            public OrderedTypeBeforeHook(int order, List<string> log, string name)
            {
                Order = order;
                _log = log;
                _name = name;
            }

            public UniTask<HookResult> OnBeforeAsync(TestContext context)
            {
                _log.Add($"Before:{_name}");
                return UniTask.FromResult(HookResult.Continue);
            }
        }

        [HookTarget("TestPipeline", nameof(DerivedPhase))]
        private class OrderedTypeAfterHook : IAfterPhaseHook<TestContext>
        {
            public int Order { get; }
            private readonly List<string> _log;
            private readonly string _name;

            public OrderedTypeAfterHook(int order, List<string> log, string name)
            {
                Order = order;
                _log = log;
                _name = name;
            }

            public UniTask OnAfterAsync(TestContext context)
            {
                _log.Add($"After:{_name}");
                return UniTask.CompletedTask;
            }
        }

        [Test]
        public async Task Pipeline_ExecutesPhases_InOrder()
        {
            var log = new List<string>();
            var pipeline = new TestPipeline { SilentMode = true };

            pipeline.AddPhase(new RecordingPhase("C", 300, log));
            pipeline.AddPhase(new RecordingPhase("A", 100, log));
            pipeline.AddPhase(new RecordingPhase("B", 200, log));

            await pipeline.ExecuteAsync(new TestContext());

            Assert.AreEqual(new[] { "Phase:A", "Phase:B", "Phase:C" }, log.ToArray());
        }

        [Test]
        public async Task Pipeline_WithNoPhases_CompletesWithoutAbort()
        {
            var pipeline = new TestPipeline { SilentMode = true };
            var context = new TestContext();

            await pipeline.ExecuteAsync(context);

            Assert.IsFalse(context.Aborted);
        }

        [Test]
        public async Task Pipeline_PhaseException_AbortsExecution()
        {
            var log = new List<string>();
            var pipeline = new TestPipeline { SilentMode = true };

            pipeline.AddPhase(new RecordingPhase("A", 100, log));
            pipeline.AddPhase(new ThrowingPhase(200));
            pipeline.AddPhase(new RecordingPhase("C", 300, log));

            LogAssert.Expect(LogType.Error, new Regex(".*ThrowingPhase.*"));

            var context = new TestContext();
            await pipeline.ExecuteAsync(context);

            Assert.IsTrue(context.Aborted);
            Assert.AreEqual(new[] { "Phase:A" }, log.ToArray());
        }

        [Test]
        public async Task Hook_MultiTarget_ExecutesBeforeAndAfter()
        {
            var log = new List<string>();
            var pipeline = new TestPipeline { SilentMode = true };

            pipeline.AddPhase(new PhaseA(log));
            pipeline.RegisterHook(new GlobalHook(log));

            await pipeline.ExecuteAsync(new TestContext());

            Assert.AreEqual(new[] { "Before:Global", "Phase:A", "After:Global" }, log.ToArray());
        }

        [Test]
        public async Task Hook_TargetSpecificPhase_OnlyMatchesTarget()
        {
            var log = new List<string>();
            var pipeline = new TestPipeline { SilentMode = true };

            pipeline.AddPhase(new PhaseA(log));
            pipeline.AddPhase(new PhaseB(log));
            pipeline.RegisterHook(new PhaseBHook(log));

            await pipeline.ExecuteAsync(new TestContext());

            Assert.AreEqual(new[] { "Phase:A", "Before:B", "Phase:B", "After:B" }, log.ToArray());
        }

        [Test]
        public async Task Hook_SkipPhase_SkipsCurrentPhase()
        {
            var log = new List<string>();
            var pipeline = new TestPipeline { SilentMode = true };

            pipeline.AddPhase(new PhaseA(log));
            pipeline.AddPhase(new PhaseB(log));
            pipeline.RegisterHook(new SkipPhaseHook(log));
            pipeline.RegisterHook(new PhaseBAfterHook(log));

            await pipeline.ExecuteAsync(new TestContext());

            Assert.AreEqual(new[] { "Phase:A", "Before:SkipPhase", "After:B" }, log.ToArray());
        }

        [Test]
        public async Task Hook_Abort_StopsPipeline()
        {
            var log = new List<string>();
            var pipeline = new TestPipeline { SilentMode = true };

            pipeline.AddPhase(new PhaseA(log));
            pipeline.AddPhase(new PhaseB(log));
            pipeline.RegisterHook(new AbortHook(log));

            var context = new TestContext();
            await pipeline.ExecuteAsync(context);

            Assert.IsTrue(context.Aborted);
            Assert.AreEqual(new[] { "Phase:A", "Before:Abort" }, log.ToArray());
        }

        [Test]
        public async Task TypeHooks_Match_SamePhaseId()
        {
            var log = new List<string>();
            var pipeline = new TestPipeline { SilentMode = true };

            pipeline.AddPhase(new DerivedPhase(log));
            pipeline.RegisterHook(new BaseClassBeforeHook(0, log));
            pipeline.RegisterHook(new InterfaceBeforeHook(0, log));
            pipeline.RegisterHook(new ConcreteBeforeHook(0, log));

            await pipeline.ExecuteAsync(new TestContext());

            var phaseIndex = log.IndexOf("Phase:Base");
            Assert.IsTrue(phaseIndex >= 0);
            Assert.Less(log.IndexOf("Before:BaseClass"), phaseIndex);
            Assert.Less(log.IndexOf("Before:Interface"), phaseIndex);
            Assert.Less(log.IndexOf("Before:Concrete"), phaseIndex);
        }

        [Test]
        public async Task TypeHooks_SkipHooks_SkipsRemainingBeforeHooks()
        {
            var log = new List<string>();
            var pipeline = new TestPipeline { SilentMode = true };

            pipeline.AddPhase(new DerivedPhase(log));
            pipeline.RegisterHook(new SkipHooksTypeHook(0, log));
            pipeline.RegisterHook(new OrderedTypeBeforeHook(1, log, "Second"));
            pipeline.RegisterHook(new OrderedTypeAfterHook(0, log, "After"));

            await pipeline.ExecuteAsync(new TestContext());

            Assert.Contains("Before:SkipHooks", log);
            Assert.IsFalse(log.Contains("Before:Second"));
            Assert.Contains("After:After", log);
        }

        [Test]
        public async Task TypeHooks_ManualOrder_SortedByOrder()
        {
            var log = new List<string>();
            var pipeline = new TestPipeline { SilentMode = true };

            pipeline.AddPhase(new DerivedPhase(log));
            pipeline.RegisterHook(new OrderedTypeBeforeHook(200, log, "B"));
            pipeline.RegisterHook(new OrderedTypeBeforeHook(100, log, "A"));
            pipeline.RegisterHook(new OrderedTypeBeforeHook(150, log, "C"));

            await pipeline.ExecuteAsync(new TestContext());

            Assert.AreEqual(new[] { "Before:A", "Before:C", "Before:B", "Phase:Base" }, log.ToArray());
        }

        [Test]
        public async Task Hook_NonGenericHooks_Execute()
        {
            var log = new List<string>();
            var pipeline = new TestPipeline { SilentMode = true };

            pipeline.AddPhase(new PhaseA(log));
            pipeline.AddHook("A", new NonGenericHook(log));
            pipeline.AddBeforeHook("A", new NonGenericBeforeHook(log));
            pipeline.AddAfterHook("A", new NonGenericAfterHook(log));

            await pipeline.ExecuteAsync(new TestContext());

            Assert.AreEqual(new[]
            {
                "Before:NonGenericBoth",
                "Before:NonGeneric",
                "Phase:A",
                "After:NonGenericBoth",
                "After:NonGeneric"
            }, log.ToArray());
        }

        [Test]
        public async Task ManualPhase_CustomId_AndOrderOverride()
        {
            var log = new List<string>();
            var pipeline = new TestPipeline { SilentMode = true };

            pipeline.AddPhase(new RecordingPhase("First", 200, log), "PhaseX", 200);
            pipeline.AddPhase(new RecordingPhase("Second", 100, log), "PhaseY", 100);
            pipeline.AddBeforeHook("PhaseX", new ManualIdHook(0, log, "A"), 200);
            pipeline.AddBeforeHook("PhaseX", new ManualIdHook(0, log, "B"), 100);

            await pipeline.ExecuteAsync(new TestContext());

            Assert.AreEqual(new[] { "Phase:Second", "Before:B", "Before:A", "Phase:First" }, log.ToArray());
        }
    }
}
