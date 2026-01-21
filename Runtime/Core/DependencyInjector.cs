using System;
using System.Collections.Generic;
using System.Reflection;
using Azathrix.Framework.Core.Attributes;
using Azathrix.Framework.Interfaces;
using Azathrix.Framework.Tools;

namespace Azathrix.Framework.Core
{
    /// <summary>
    /// 依赖注入器 - 负责解析和注入系统依赖
    /// </summary>
    public class DependencyInjector
    {
        /// <summary>
        /// 注入信息缓存（避免重复反射）
        /// </summary>
        private readonly Dictionary<Type, InjectionInfo> _injectionCache = new();

        /// <summary>
        /// 系统容器引用（用于获取依赖实例）
        /// </summary>
        private readonly SystemContainer _container;

        /// <summary>
        /// 是否为编辑器模式（控制日志输出）
        /// </summary>
        public bool IsEditorMode { get; set; }

        /// <summary>
        /// 创建依赖注入器
        /// </summary>
        /// <param name="container">系统容器</param>
        public DependencyInjector(SystemContainer container)
        {
            _container = container;
        }

        /// <summary>
        /// 为所有已注册系统执行依赖注入
        /// </summary>
        public void InjectAll()
        {
            foreach (var system in _container.Systems)
                InjectTo(system);
        }

        /// <summary>
        /// 向任意对象注入依赖
        /// </summary>
        /// <param name="target">目标对象</param>
        /// <remarks>
        /// 扫描目标对象的所有字段和属性，
        /// 对标记了 [Inject] 或 [WeakInject] 特性的成员进行依赖注入。
        /// [Inject] 为强依赖，未找到时输出警告；
        /// [WeakInject] 为弱依赖，未找到时静默跳过。
        /// </remarks>
        public void InjectTo(object target)
        {
            if (target == null) return;

            var type = target.GetType();
            var info = GetOrCreateInjectionInfo(type);
            var injectedCount = 0;

            // 注入字段
            foreach (var (field, isWeak) in info.Fields)
            {
                if (_container.TypeToInstance.TryGetValue(field.FieldType, out var dep))
                {
                    field.SetValue(target, dep);
                    injectedCount++;
                }
                else if (!isWeak)
                {
                    Log.Warning($"[Inject] {type.Name}.{field.Name}: 未找到 {field.FieldType.Name}");
                }
            }

            // 注入属性
            foreach (var (prop, isWeak) in info.Properties)
            {
                if (_container.TypeToInstance.TryGetValue(prop.PropertyType, out var dep))
                {
                    prop.SetValue(target, dep);
                    injectedCount++;
                }
                else if (!isWeak)
                {
                    Log.Warning($"[Inject] {type.Name}.{prop.Name}: 未找到 {prop.PropertyType.Name}");
                }
            }

            if (injectedCount > 0 && !IsEditorMode)
                Log.Info($"[Inject] {type.Name}: 注入 {injectedCount} 个依赖");
        }

        /// <summary>
        /// 获取或创建类型的注入信息（带缓存）
        /// </summary>
        /// <param name="type">目标类型</param>
        /// <returns>注入信息</returns>
        private InjectionInfo GetOrCreateInjectionInfo(Type type)
        {
            if (_injectionCache.TryGetValue(type, out var info))
                return info;

            info = new InjectionInfo();
            var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

            // 扫描字段
            foreach (var field in type.GetFields(flags))
            {
                var isInject = field.GetCustomAttribute<InjectAttribute>() != null;
                var isWeakInject = field.GetCustomAttribute<WeakInjectAttribute>() != null;
                if (isInject || isWeakInject)
                    info.Fields.Add((field, isWeakInject));
            }

            // 扫描属性
            foreach (var prop in type.GetProperties(flags))
            {
                if (!prop.CanWrite) continue;
                var isInject = prop.GetCustomAttribute<InjectAttribute>() != null;
                var isWeakInject = prop.GetCustomAttribute<WeakInjectAttribute>() != null;
                if (isInject || isWeakInject)
                    info.Properties.Add((prop, isWeakInject));
            }

            _injectionCache[type] = info;
            return info;
        }

        /// <summary>
        /// 注入信息 - 缓存类型的可注入成员
        /// </summary>
        private class InjectionInfo
        {
            /// <summary>
            /// 可注入的字段列表（字段信息, 是否为弱依赖）
            /// </summary>
            public List<(FieldInfo field, bool isWeak)> Fields = new();

            /// <summary>
            /// 可注入的属性列表（属性信息, 是否为弱依赖）
            /// </summary>
            public List<(PropertyInfo prop, bool isWeak)> Properties = new();
        }
    }
}
