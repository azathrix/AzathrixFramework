using System;
using System.Reflection;
using System.Threading.Tasks;
using Azathrix.Framework.Core.Pipeline;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Azathrix.Framework.Samples.Pipeline.Tests
{
    public class SamplePipelineTests
    {
        private static Type FindType(string fullName)
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                var type = assembly.GetType(fullName);
                if (type != null)
                    return type;
            }
            return null;
        }

        [Test]
        public async Task SamplePipeline_NormalFlow_EmitsLogs()
        {
            var runtimeType = FindType("Azathrix.Framework.Samples.Pipeline.SamplePipelineRuntime");
            Assert.NotNull(runtimeType, "SamplePipelineRuntime 未找到");

            var requestRun = runtimeType.GetMethod("RequestRun", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            requestRun?.Invoke(null, null);

            var runAsync = runtimeType.GetMethod("RunAsync", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            Assert.NotNull(runAsync, "RunAsync 未找到");

            LogAssert.Expect(LogType.Log, "[PipelineSample] Before:Sample");
            LogAssert.Expect(LogType.Log, "[PipelineSample] Phase:Sample");
            LogAssert.Expect(LogType.Log, "[PipelineSample] After:Sample");

            var task = (UniTask)runAsync.Invoke(null, null);
            await task;
        }

        [Test]
        public void SamplePipeline_CanResolveFromFactory()
        {
            var pipeline = PipelineFactory.Get("SamplePipeline");
            Assert.NotNull(pipeline);
            Assert.AreEqual("SamplePipeline", pipeline.Id);
        }
    }
}
