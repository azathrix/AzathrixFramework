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

        // 手动维护：只扫描指定前缀（留空则不过滤）
        private static readonly string[] ManualIncludePrefixes = { };

        // 手动维护：额外排除的前缀
        private static readonly string[] ManualExcludePrefixes = { };

        // 手动维护：精确排除的程序集名
        private static readonly string[] ManualExcludeNames = { };

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

            if (ManualIncludePrefixes.Length > 0 &&
                !ManualIncludePrefixes.Any(p => name.StartsWith(p, StringComparison.OrdinalIgnoreCase)))
                return false;

            if (ManualExcludePrefixes.Any(p => name.StartsWith(p, StringComparison.OrdinalIgnoreCase)))
                return false;

            if (ManualExcludeNames.Any(n => string.Equals(n, name, StringComparison.OrdinalIgnoreCase)))
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
