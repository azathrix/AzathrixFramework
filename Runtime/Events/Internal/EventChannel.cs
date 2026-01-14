using Azathrix.Framework.Events.Core;
using Azathrix.Framework.Events.Interceptors;

namespace Azathrix.Framework.Events.Internal
{
    /// <summary>
    /// 泛型事件通道（避免装箱）
    /// </summary>
    internal sealed class EventChannel<T> where T : struct
    {
        private readonly EventDispatcher _dispatcher;
        private readonly HandlerList<T> _handlers;
        private readonly InterceptorList<T> _interceptors;

        // Sticky 事件支持
        private T _stickyValue;
        private bool _hasSticky;

        public EventChannel(EventDispatcher dispatcher)
        {
            _dispatcher = dispatcher;
            _handlers = new HandlerList<T>();
            _interceptors = new InterceptorList<T>();
        }

        /// <summary>
        /// 订阅事件
        /// </summary>
        public uint Subscribe(Handlers.EventCallback<T> handler, int priority = 0)
        {
            var id = _dispatcher.GenerateId();
            _handlers.Add(id, handler, priority);
            return id;
        }

        /// <summary>
        /// 取消订阅（同时检查处理器和拦截器）
        /// </summary>
        public void Unsubscribe(uint id)
        {
            _handlers.Remove(id);
            _interceptors.Remove(id);
        }

        /// <summary>
        /// 设置优先级
        /// </summary>
        public void SetPriority(uint id, int priority)
        {
            _handlers.SetPriority(id, priority);
        }

        /// <summary>
        /// 设置一次性
        /// </summary>
        public void SetOnce(uint id)
        {
            _handlers.SetOnce(id);
        }

        /// <summary>
        /// 分发事件
        /// </summary>
        public void Dispatch(ref T evt, object sender = null)
        {
            // 在整个分发周期开始前标记，包括拦截器处理
            _handlers.BeginDispatch();
            try
            {
                // 先经过拦截器
                if (_interceptors.Count > 0)
                {
                    var ctx = new InterceptorContext<T>(ref evt, sender);
                    var result = _interceptors.Process(ref ctx);
                    if (result == InterceptResult.Cancel)
                        return;
                    evt = ctx.Event;
                }

                _handlers.DispatchInternal(ref evt);
            }
            finally
            {
                _handlers.EndDispatch();
            }
        }

        /// <summary>
        /// 添加拦截器
        /// </summary>
        public uint AddInterceptor(InterceptorHandler<T> handler, int priority = 0)
        {
            var id = _dispatcher.GenerateId();
            _interceptors.Add(id, handler, priority);
            return id;
        }

        /// <summary>
        /// 移除拦截器
        /// </summary>
        public void RemoveInterceptor(uint id)
        {
            _interceptors.Remove(id);
        }

        /// <summary>
        /// 订阅者数量
        /// </summary>
        public int SubscriberCount => _handlers.Count;

        /// <summary>
        /// 拦截器数量
        /// </summary>
        public int InterceptorCount => _interceptors.Count;

        /// <summary>
        /// 清空
        /// </summary>
        public void Clear()
        {
            _handlers.Clear();
            _interceptors.Clear();
        }

        /// <summary>
        /// 设置 Sticky 值
        /// </summary>
        public void SetSticky(ref T evt)
        {
            _stickyValue = evt;
            _hasSticky = true;
        }

        /// <summary>
        /// 清除 Sticky 值
        /// </summary>
        public void ClearSticky()
        {
            _stickyValue = default;
            _hasSticky = false;
        }

        /// <summary>
        /// 是否有 Sticky 值
        /// </summary>
        public bool HasSticky => _hasSticky;

        /// <summary>
        /// 获取 Sticky 值
        /// </summary>
        public ref T GetStickyValue() => ref _stickyValue;
    }
}
