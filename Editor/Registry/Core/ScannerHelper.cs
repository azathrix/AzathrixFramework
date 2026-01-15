using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Azathrix.Framework.Editor.Registry
{
    /// <summary>
    /// 程序集扫描辅助类
    /// </summary>
    public static class ScannerHelper
    {
        private static readonly string[] ExcludePrefixes =
        {
            "System", "Microsoft", "Unity", "mscorlib", "netstandard", "Mono", "nunit"
        };

        public static IEnumerable<Assembly> GetAssemblies()
        {
            return AppDomain.CurrentDomain.GetAssemblies()
                .Where(ShouldScanAssembly);
        }

        public static bool ShouldScanAssembly(Assembly assembly)
        {
            var name = assembly.GetName().Name;

            // 跳过 Unity 热重载产生的临时程序集
            if (name.Contains("-") && name.Length > 50)
                return false;

            // 排除系统程序集
            if (ExcludePrefixes.Any(p => name.StartsWith(p)))
                return false;

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
