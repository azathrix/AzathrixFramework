using System.Collections.Generic;
using System.Threading.Tasks;
using Azathrix.Framework.Core.Pipeline;
using Cysharp.Threading.Tasks;
using NUnit.Framework;

namespace Azathrix.Framework.Tests
{
    public class PipelineFactoryTests
    {
        public class FactoryContext : PipelineContext { }

        public interface IFactoryPhase : IPhase<FactoryContext> { }

        public class FactoryPipeline : PipelineBase<IFactoryPhase, FactoryContext> { }

        public class FactoryPhase : IFactoryPhase
        {
            public int Order => 100;
            private readonly List<string> _log;

            public FactoryPhase(List<string> log) => _log = log;

            public UniTask ExecuteAsync(FactoryContext context)
            {
                _log.Add("Phase:Factory");
                return UniTask.CompletedTask;
            }
        }

        [Test]
        public async Task Factory_CreateEmpty_ByType_AllowsManualBuild()
        {
            var log = new List<string>();
            var pipeline = PipelineFactory.CreateEmpty<FactoryPipeline>();

            Assert.NotNull(pipeline);
            pipeline.SilentMode = true;
            pipeline.AddPhase(new FactoryPhase(log));

            await pipeline.ExecuteAsync(new FactoryContext());

            Assert.AreEqual(new[] { "Phase:Factory" }, log.ToArray());
        }

        [Test]
        public async Task Factory_CreateEmpty_ById_AllowsManualBuild()
        {
            var log = new List<string>();
            var pipeline = PipelineFactory.CreateEmpty("FactoryPipeline") as FactoryPipeline;

            Assert.NotNull(pipeline);
            pipeline.SilentMode = true;
            pipeline.AddPhase(new FactoryPhase(log));

            await pipeline.ExecuteAsync(new FactoryContext());

            Assert.AreEqual(new[] { "Phase:Factory" }, log.ToArray());
        }
    }
}
