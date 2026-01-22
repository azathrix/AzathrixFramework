using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Azathrix.Framework.Interfaces;

namespace Azathrix.Framework.Core
{
    /// <summary>
    /// 系统性能分析器 - 负责系统的性能统计和监控
    /// </summary>
    public class SystemProfiler
    {
        /// <summary>
        /// 性能数据存储
        /// </summary>
        private readonly Dictionary<ISystem, PerformanceData> _performanceData = new();

        /// <summary>
        /// 计时器
        /// </summary>
        private readonly Stopwatch _stopwatch = new();

        /// <summary>
        /// 是否启用性能统计
        /// </summary>
        public bool Enabled { get; set; }

        /// <summary>
        /// 为系统注册性能数据
        /// </summary>
        /// <param name="system">系统实例</param>
        public void Register(ISystem system)
        {
            if (!_performanceData.ContainsKey(system))
                _performanceData[system] = new PerformanceData();
        }

        /// <summary>
        /// 移除系统的性能数据
        /// </summary>
        /// <param name="system">系统实例</param>
        public void Unregister(ISystem system)
        {
            _performanceData.Remove(system);
        }

        /// <summary>
        /// 开始计时
        /// </summary>
        public void BeginSample()
        {
            if (Enabled)
                _stopwatch.Restart();
        }

        /// <summary>
        /// 结束计时并记录
        /// </summary>
        /// <param name="system">系统实例</param>
        public void EndSample(ISystem system)
        {
            if (!Enabled) return;

            _stopwatch.Stop();
            if (_performanceData.TryGetValue(system, out var data))
                data.Record(_stopwatch.Elapsed.TotalMilliseconds);
        }

        /// <summary>
        /// 获取系统的最后一次更新耗时（毫秒）
        /// </summary>
        public double GetLastMs(ISystem system)
        {
            return _performanceData.TryGetValue(system, out var data) ? data.LastMs : 0;
        }

        /// <summary>
        /// 获取系统的平均更新耗时（毫秒）
        /// </summary>
        public double GetAverageMs(ISystem system)
        {
            return _performanceData.TryGetValue(system, out var data) ? data.AverageMs : 0;
        }

        /// <summary>
        /// 清除所有性能数据
        /// </summary>
        public void Clear()
        {
            _performanceData.Clear();
        }

        /// <summary>
        /// 性能数据 - 记录系统的执行时间
        /// </summary>
        private class PerformanceData
        {
            /// <summary>
            /// 采样数量（用于计算平均值）
            /// </summary>
            private const int SampleCount = 60;

            /// <summary>
            /// 采样队列
            /// </summary>
            private readonly Queue<double> _samples = new();

            /// <summary>
            /// 最后一次执行耗时（毫秒）
            /// </summary>
            public double LastMs { get; private set; }

            /// <summary>
            /// 平均执行耗时（毫秒）
            /// </summary>
            public double AverageMs => _samples.Count > 0 ? _samples.Average() : 0;

            /// <summary>
            /// 记录一次执行耗时
            /// </summary>
            /// <param name="ms">耗时（毫秒）</param>
            public void Record(double ms)
            {
                LastMs = ms;
                _samples.Enqueue(ms);
                if (_samples.Count > SampleCount)
                    _samples.Dequeue();
            }
        }
    }
}
