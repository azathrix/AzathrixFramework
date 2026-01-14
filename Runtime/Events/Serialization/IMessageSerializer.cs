using System;

namespace Azathrix.Framework.Events.Serialization
{
    /// <summary>
    /// 消息序列化器接口
    /// </summary>
    public interface IMessageSerializer
    {
        /// <summary>
        /// 序列化数据
        /// </summary>
        int Serialize<T>(T data, Span<byte> buffer);

        /// <summary>
        /// 反序列化数据
        /// </summary>
        T Deserialize<T>(ReadOnlySpan<byte> data);

        /// <summary>
        /// 获取序列化后的大小（可选，返回-1表示未知）
        /// </summary>
        int GetSerializedSize<T>(T data) => -1;
    }

    /// <summary>
    /// 默认JSON序列化器（使用Unity的JsonUtility）
    /// </summary>
    public sealed class JsonMessageSerializer : IMessageSerializer
    {
        public int Serialize<T>(T data, Span<byte> buffer)
        {
            var json = UnityEngine.JsonUtility.ToJson(data);
            var bytes = System.Text.Encoding.UTF8.GetBytes(json);
            bytes.CopyTo(buffer);
            return bytes.Length;
        }

        public T Deserialize<T>(ReadOnlySpan<byte> data)
        {
            var json = System.Text.Encoding.UTF8.GetString(data);
            return UnityEngine.JsonUtility.FromJson<T>(json);
        }
    }
}
