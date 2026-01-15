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
    public class HookEdgeCaseTests
    {
        private class TestContext : PipelineContext { }

        private interface ITestPhase : IPhase<TestContext> { }

        private class TestPipeline : PipelineBase<ITestPhase, TestContext> { }

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

        [HookTarget("TestPipeline", "A")]
        [HookTarget("TestPipeline", "B")]
        private class ThrowingBeforeHook : IBeforePhaseHook<TestContext>
        {
            public int Order => 0;

            public UniTask<HookResult> OnBeforeAsync(TestContext context)
            {
                throw new Exception("Test before exception");
            }
        }

        [HookTarget("TestPipeline", "A")]
        [HookTarget("TestPipeline", "B")]
        private class ThrowingAfterHook : IAfterPhaseHook<TestContext>
        {
            public int Order => 0;

            public UniTask OnAfterAsync(TestContext context)
            {
                throw new Exception("Test after exception");
            }
        }

        [HookTarget("TestPipeline", "A")]
        private class SkipAllHook : IBeforePhaseHook<TestContext>
        {
            public int Order => 0;

            public UniTask<HookResult> OnBeforeAsync(TestContext context)
            {
                return UniTask.FromResult(HookResult.SkipAll);
            }
        }

        [HookTarget("TestPipeline", "A")]
        private class SkipHooksHook : IBeforePhaseHook<TestContext>
        {
            public int Order => 0;
            private readonly List<string> _log;

            public SkipHooksHook(List<string> log) => _log = log;

            public UniTask<HookResult> OnBeforeAsync(TestContext context)
            {
                _log.Add("Before:SkipHooks");
                return UniTask.FromResult(HookResult.SkipHooks);
            }
        }

        [HookTarget("TestPipeline", "A")]
        private class NamedBeforeHook : IBeforePhaseHook<TestContext>
        {
            public int Order { get; }
            private readonly List<string> _log;
            private readonly string _name;

            public NamedBeforeHook(int order, List<string> log, string name)
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

        [HookTarget("TestPipeline", "A")]
        private class NamedAfterHook : IAfterPhaseHook<TestContext>
        {
            public int Order { get; }
            private readonly List<string> _log;
            private readonly string _name;

            public NamedAfterHook(int order, List<string> log, string name)
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

        [HookTarget("TestPipeline", "MissingPhase")]
        private class NeverMatchHook : IHook<TestContext>
        {
            public int Order => 0;
            private readonly Action _callback;

            public NeverMatchHook(Action callback) => _callback = callback;

            public UniTask<HookResult> OnBeforeAsync(TestContext context)
            {
                _callback();
                return UniTask.FromResult(HookResult.Continue);
            }

            public UniTask OnAfterAsync(TestContext context)
            {
                _callback();
                return UniTask.CompletedTask;
            }
        }

        [Test]
        public async Task Hook_ThrowsInBefore_ContinuesExecution()
        {
            var log = new List<string>();
            var pipeline = new TestPipeline { SilentMode = true };

            pipeline.AddPhase(new PhaseA(log));
            pipeline.AddPhase(new PhaseB(log));
            pipeline.RegisterHook(new ThrowingBeforeHook());
            pipeline.RegisterHook(new NamedBeforeHook(1, log, "Next"));

            LogAssert.Expect(LogType.Error, new Regex(".*ThrowingBeforeHook.*"));
            LogAssert.Expect(LogType.Error, new Regex(".*ThrowingBeforeHook.*"));

            var context = new TestContext();
            await pipeline.ExecuteAsync(context);

            Assert.IsFalse(context.Aborted);
            Assert.Contains("Before:Next", log);
        }

        [Test]
        public async Task Hook_ThrowsInAfter_ContinuesExecution()
        {
            var log = new List<string>();
            var pipeline = new TestPipeline { SilentMode = true };

            pipeline.AddPhase(new PhaseA(log));
            pipeline.AddPhase(new PhaseB(log));
            pipeline.RegisterHook(new ThrowingAfterHook());
            pipeline.RegisterHook(new NamedAfterHook(1, log, "Next"));

            LogAssert.Expect(LogType.Error, new Regex(".*ThrowingAfterHook.*"));
            LogAssert.Expect(LogType.Error, new Regex(".*ThrowingAfterHook.*"));

            var context = new TestContext();
            await pipeline.ExecuteAsync(context);

            Assert.IsFalse(context.Aborted);
            Assert.Contains("After:Next", log);
        }

        [Test]
        public async Task Hook_MatchNone_NeverCalled()
        {
            var called = false;
            var pipeline = new TestPipeline { SilentMode = true };

            pipeline.AddPhase(new PhaseA(new List<string>()));
            pipeline.RegisterHook(new NeverMatchHook(() => called = true));

            await pipeline.ExecuteAsync(new TestContext());

            Assert.IsFalse(called);
        }

        [Test]
        public async Task Hook_SkipAll_AfterHookNotCalled()
        {
            var log = new List<string>();
            var pipeline = new TestPipeline { SilentMode = true };

            pipeline.AddPhase(new PhaseA(log));
            pipeline.RegisterHook(new SkipAllHook());
            pipeline.RegisterHook(new NamedAfterHook(1, log, "After"));

            await pipeline.ExecuteAsync(new TestContext());

            Assert.IsFalse(log.Contains("After:After"));
        }

        [Test]
        public async Task Hook_SkipHooks_SkipsRemainingBeforeHooks()
        {
            var log = new List<string>();
            var pipeline = new TestPipeline { SilentMode = true };

            pipeline.AddPhase(new PhaseA(log));
            pipeline.RegisterHook(new NamedBeforeHook(0, log, "H1"));
            pipeline.RegisterHook(new SkipHooksHook(log));
            pipeline.RegisterHook(new NamedBeforeHook(2, log, "H2"));
            pipeline.RegisterHook(new NamedAfterHook(0, log, "After"));

            await pipeline.ExecuteAsync(new TestContext());

            Assert.Contains("Before:H1", log);
            Assert.IsFalse(log.Contains("Before:H2"));
            Assert.Contains("After:After", log);
        }
    }
}
