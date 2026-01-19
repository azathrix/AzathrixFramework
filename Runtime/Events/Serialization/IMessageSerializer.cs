using System;

namespace Azathrix.Framework.Events.Serialization
{
    /// <summary>
    /// 消息序列化器接口
    /// </summary>
    /// <remarks>
    /// 用于消息事件的跨线程/跨进程传输时的序列化和反序列化
    /// </remarks>
    public interface IMessageSerializer
    {
        /// <summary>
        /// 序列化数据到字节缓冲区
        /// </summary>
        /// <typeparam name="T">数据类型</typeparam>
        /// <param name="data">要序列化的数据</param>
        /// <param name="buffer">目标缓冲区</param>
        /// <returns>写入的字节数</returns>
        int Serialize<T>(T data, Span<byte> buffer);

        /// <summary>
        /// 从字节数据反序列化
        /// </summary>
        /// <typeparam name="T">目标类型</typeparam>
        /// <param name="data">字节数据</param>
        /// <returns>反序列化后的对象</returns>
        T Deserialize<T>(ReadOnlySpan<byte> data);

        /// <summary>
        /// 获取序列化后的大小（可选，返回-1表示未知）
        /// </summary>
        /// <typeparam name="T">数据类型</typeparam>
        /// <param name="data">要序列化的数据</param>
        /// <returns>预估的字节数，-1表示未知</returns>
        int GetSerializedSize<T>(T data) => -1;
    }

    /// <summary>
    /// 默认JSON序列化器（使用Unity的JsonUtility）
    /// </summary>
    /// <remarks>
    /// 简单实现，适用于基本类型。生产环境建议使用更高效的序列化器如MessagePack。
    /// </remarks>
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
