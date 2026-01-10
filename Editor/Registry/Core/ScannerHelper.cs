using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Azathrix.Framework.Core.Configs;

namespace Azathrix.Framework.Editor.Registry
{
    public static class ScannerHelper
    {
        public static IEnumerable<Assembly> GetAssemblies(ScannerConfig config)
        {
            if (config?.Assemblies?.Count > 0)
                return config.Assemblies;

            return AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => ShouldScanAssembly(a, config));
        }

        public static bool ShouldScanAssembly(Assembly assembly, ScannerConfig config)
        {
            var name = assembly.GetName().Name;

            // 跳过 Unity 热重载产生的临时程序集
            if (name.Contains("-") && name.Length > 50)
                return false;

            // 排除系统程序集
            if (config?.ExcludeAssemblyPrefixes != null)
            {
                if (config.ExcludeAssemblyPrefixes.Any(p => name.StartsWith(p)))
                    return false;
            }

            // 如果指定了前缀过滤
            if (config?.AssemblyPrefixes?.Count > 0)
                return config.AssemblyPrefixes.Any(p => name.StartsWith(p));

            return true;
        }

        public static Type[] GetTypes(Assembly assembly)
        {
            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException e)
            {
                return e.Types.Where(t => t != null).ToArray();
            }
        }
    }
}
