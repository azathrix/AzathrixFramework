using Cysharp.Threading.Tasks;

namespace Azathrix.Framework.Events.Handlers
{
    /// <summary>
    /// 同步事件处理器委托
    /// </summary>
    public delegate void EventCallback<T>(ref T evt) where T : struct;

    /// <summary>
    /// 异步事件处理器委托
    /// </summary>
    public delegate UniTask AsyncEventHandler<T>(T evt) where T : struct;

    /// <summary>
    /// 带返回值的事件处理器委托
    /// </summary>
    public delegate TResult QueryHandler<T, out TResult>(ref T evt) where T : struct;

    /// <summary>
    /// 事件初始化器委托（用于struct事件的初始化）
    /// </summary>
    public delegate void EventInitializer<T>(ref T evt) where T : struct;

    /// <summary>
    /// 事件过滤器委托
    /// </summary>
    public delegate bool EventFilter<T>(ref T evt) where T : struct;
}
