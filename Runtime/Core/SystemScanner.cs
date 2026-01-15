using System;
using System.Linq;
using Azathrix.Framework.Interfaces;
using Azathrix.Framework.Registry;
using Cysharp.Threading.Tasks;

namespace Azathrix.Framework.Core
{
    /// <summary>
    /// 游戏系统扫描器（从注册表读取）
    /// </summary>
    public class SystemScanner
    {
        private readonly ILogger _logger;

        public SystemScanner(ILogger logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// 异步扫描所有符合条件的游戏系统类型
        /// </summary>
        public async UniTask<Type[]> ScanAsync()
        {
            var registry = SystemRegistry.Instance;
            if (registry != null && registry.entries.Count > 0)
            {
                var types = registry.GetEnabledTypes()
                    .Where(t => IsValidSystemType(t))
                    .ToArray();

                _logger.Info($"[Scanner] 从 SystemRegistry 加载 {types.Length} 个系统");
                await UniTask.Yield();
                return types;
            }

            _logger.Error("[Scanner] SystemRegistry 为空或未初始化，无法加载系统");
            await UniTask.Yield();
            return Array.Empty<Type>();
        }

        private bool IsValidSystemType(Type type)
        {
            if (!typeof(ISystem).IsAssignableFrom(type))
                return false;
            if (type.IsAbstract || type.IsInterface)
                return false;

            var registry = SystemRegistry.Instance;
            if (registry != null && registry.IsSystemDisabled(type))
            {
                _logger.Info($"系统被禁用: {type.FullName}");
                return false;
            }

            return true;
        }
    }
}
