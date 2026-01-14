using System;

namespace Azathrix.Framework.Events.Interceptors
{
    /// <summary>
    /// 拦截器信息
    /// </summary>
    internal struct Interceptor<T> where T : struct
    {
        public uint Id;
        public int Priority;
        public bool Removed;
        public InterceptorHandler<T> Handler;
    }

    /// <summary>
    /// 拦截器列表
    /// </summary>
    internal sealed class InterceptorList<T> where T : struct
    {
        private Interceptor<T>[] _interceptors;
        private int _count;
        private bool _needsSort;

        public InterceptorList(int initialCapacity = 4)
        {
            _interceptors = new Interceptor<T>[initialCapacity];
            _count = 0;
        }

        public int Count => _count;

        /// <summary>
        /// 添加拦截器
        /// </summary>
        public void Add(uint id, InterceptorHandler<T> handler, int priority = 0)
        {
            // 扩容
            if (_count >= _interceptors.Length)
            {
                var newArray = new Interceptor<T>[_interceptors.Length * 2];
                Array.Copy(_interceptors, newArray, _count);
                _interceptors = newArray;
            }

            _interceptors[_count++] = new Interceptor<T>
            {
                Id = id,
                Priority = priority,
                Removed = false,
                Handler = handler
            };
            _needsSort = true;
        }

        /// <summary>
        /// 移除拦截器
        /// </summary>
        public void Remove(uint id)
        {
            for (int i = 0; i < _count; i++)
            {
                if (_interceptors[i].Id == id)
                {
                    // 用最后一个元素覆盖
                    _interceptors[i] = _interceptors[--_count];
                    _interceptors[_count] = default;
                    _needsSort = true;
                    return;
                }
            }
        }

        /// <summary>
        /// 处理拦截器链
        /// </summary>
        public InterceptResult Process(ref InterceptorContext<T> ctx)
        {
            if (_count == 0) return InterceptResult.Continue;

            // 排序
            if (_needsSort)
            {
                SortByPriority();
                _needsSort = false;
            }

            for (int i = 0; i < _count; i++)
            {
                ref var interceptor = ref _interceptors[i];
                if (interceptor.Removed) continue;

                var result = interceptor.Handler(ref ctx);
                if (result == InterceptResult.Cancel)
                    return InterceptResult.Cancel;
            }

            return InterceptResult.Continue;
        }

        /// <summary>
        /// 插入排序（按优先级降序）
        /// </summary>
        private void SortByPriority()
        {
            for (int i = 1; i < _count; i++)
            {
                var key = _interceptors[i];
                int j = i - 1;

                while (j >= 0 && _interceptors[j].Priority < key.Priority)
                {
                    _interceptors[j + 1] = _interceptors[j];
                    j--;
                }
                _interceptors[j + 1] = key;
            }
        }

        /// <summary>
        /// 清空
        /// </summary>
        public void Clear()
        {
            for (int i = 0; i < _count; i++)
                _interceptors[i] = default;
            _count = 0;
        }
    }
}
