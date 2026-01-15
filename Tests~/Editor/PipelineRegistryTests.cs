using System.Collections.Generic;
using Azathrix.Framework.Core.Pipeline;
using NUnit.Framework;
using UnityEngine;

namespace Azathrix.Framework.Tests
{
    public class PipelineRegistryTests
    {
        private PipelineRegistry _registry;

        [SetUp]
        public void SetUp()
        {
            _registry = ScriptableObject.CreateInstance<PipelineRegistry>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_registry != null)
                Object.DestroyImmediate(_registry);
        }

        [Test]
        public void GetPipeline_NotExists_ReturnsNull()
        {
            var result = _registry.GetPipeline("NonExistent");
            Assert.IsNull(result);
        }

        [Test]
        public void GetOrCreatePipeline_CreatesNew()
        {
            var pipeline = _registry.GetOrCreatePipeline("Test", "测试管线");

            Assert.IsNotNull(pipeline);
            Assert.AreEqual("Test", pipeline.pipelineId);
            Assert.AreEqual("测试管线", pipeline.displayName);
            Assert.AreEqual(1, _registry.pipelines.Count);
        }

        [Test]
        public void GetOrCreatePipeline_ReturnsSame()
        {
            var first = _registry.GetOrCreatePipeline("Test");
            var second = _registry.GetOrCreatePipeline("Test");

            Assert.AreSame(first, second);
            Assert.AreEqual(1, _registry.pipelines.Count);
        }

        [Test]
        public void IsPhaseEnabled_NotRegistered_ReturnsTrue()
        {
            var result = _registry.IsPhaseEnabled("Test", "SomePhase");
            Assert.IsTrue(result);
        }

        [Test]
        public void IsPhaseEnabled_Disabled_ReturnsFalse()
        {
            var pipeline = _registry.GetOrCreatePipeline("Test");
            pipeline.phases.Add(new PhaseEntry
            {
                typeName = "TestPhase",
                enabled = false
            });

            var result = _registry.IsPhaseEnabled("Test", "TestPhase");
            Assert.IsFalse(result);
        }

        [Test]
        public void GetOrderedPhases_ReturnsEnabledOnly()
        {
            var pipeline = _registry.GetOrCreatePipeline("Test");
            pipeline.phases.Add(new PhaseEntry { typeName = "A", order = 100, enabled = true });
            pipeline.phases.Add(new PhaseEntry { typeName = "B", order = 200, enabled = false });
            pipeline.phases.Add(new PhaseEntry { typeName = "C", order = 300, enabled = true });

            var result = _registry.GetOrderedPhases("Test");

            Assert.AreEqual(2, result.Count);
            Assert.AreEqual("A", result[0].typeName);
            Assert.AreEqual("C", result[1].typeName);
        }

        [Test]
        public void GetOrderedPhases_SortsByOrder()
        {
            var pipeline = _registry.GetOrCreatePipeline("Test");
            pipeline.phases.Add(new PhaseEntry { typeName = "C", order = 300, enabled = true });
            pipeline.phases.Add(new PhaseEntry { typeName = "A", order = 100, enabled = true });
            pipeline.phases.Add(new PhaseEntry { typeName = "B", order = 200, enabled = true });

            var result = _registry.GetOrderedPhases("Test");

            Assert.AreEqual("A", result[0].typeName);
            Assert.AreEqual("B", result[1].typeName);
            Assert.AreEqual("C", result[2].typeName);
        }

        [Test]
        public void GetHooksForPhase_FiltersCorrectly()
        {
            var pipeline = _registry.GetOrCreatePipeline("Test");
            pipeline.hooks.Add(new HookEntry { typeName = "H1", isBefore = true, enabled = true, targets = new List<HookTargetEntry> { new HookTargetEntry { phaseId = "PhaseA" } } });
            pipeline.hooks.Add(new HookEntry { typeName = "H2", isBefore = false, enabled = true, targets = new List<HookTargetEntry> { new HookTargetEntry { phaseId = "PhaseA" } } });
            pipeline.hooks.Add(new HookEntry { typeName = "H3", isBefore = true, enabled = true, targets = new List<HookTargetEntry> { new HookTargetEntry { phaseId = "PhaseB" } } });
            pipeline.hooks.Add(new HookEntry { typeName = "H4", isBefore = true, enabled = false, targets = new List<HookTargetEntry> { new HookTargetEntry { phaseId = "PhaseA" } } });

            var beforeHooks = _registry.GetHooksForPhase("Test", "PhaseA", true);
            var afterHooks = _registry.GetHooksForPhase("Test", "PhaseA", false);

            Assert.AreEqual(1, beforeHooks.Count);
            Assert.AreEqual("H1", beforeHooks[0].typeName);
            Assert.AreEqual(1, afterHooks.Count);
            Assert.AreEqual("H2", afterHooks[0].typeName);
        }

        [Test]
        public void HookEntry_TargetsMatch_EmptyTargets_ReturnsFalse()
        {
            var hook = new HookEntry
            {
                targets = new List<HookTargetEntry>()
            };

            Assert.IsFalse(hook.TargetsMatch("AnyPhase"));
        }

        [Test]
        public void PhaseEntry_IsMissing_InvalidType()
        {
            var entry = new PhaseEntry
            {
                typeName = "NonExistent.Type",
                assemblyName = "NonExistent"
            };

            Assert.IsTrue(entry.IsMissing);
        }
    }
}
