namespace Azathrix.Framework.Core.Pipeline
{
    /// <summary>
    /// 管线上下文基类
    /// </summary>
    public class PipelineContext
    {
        /// <summary>
        /// 是否已中断
        /// </summary>
        public bool Aborted { get; set; }

        /// <summary>
        /// 是否为编辑器模式
        /// </summary>
        public bool IsEditor { get; set; }

        /// <summary>
        /// 自定义数据存储
        /// </summary>
        public System.Collections.Generic.Dictionary<string, object> Data { get; } = new();

        /// <summary>
        /// 获取或设置数据
        /// </summary>
        public T Get<T>(string key, T defaultValue = default)
        {
            return Data.TryGetValue(key, out var value) ? (T)value : defaultValue;
        }

        /// <summary>
        /// 设置数据
        /// </summary>
        public void Set<T>(string key, T value)
        {
            Data[key] = value;
        }
    }
}
