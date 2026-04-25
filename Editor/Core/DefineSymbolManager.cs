#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;

namespace Azathrix.Framework.Editor
{
    /// <summary>
    /// 全局宏定义管理器
    /// </summary>
    [InitializeOnLoad]
    public static class DefineSymbolManager
    {
        static DefineSymbolManager()
        {
            EditorApplication.delayCall += SyncToPlayerSettings;
            DefineSymbolSettings.OnSettingsChanged += SyncToPlayerSettings;
        }

        /// <summary> 
        /// 添加宏定义
        /// </summary> 
        public static void Add(string symbol, string source = null)
        { 
            var settings = DefineSymbolSettings.Instance;
            if (settings == null) return;

            if (settings.Add(symbol, source))
            {
                EditorUtility.SetDirty(settings);
                SyncToPlayerSettings();
            }
        }

        /// <summary>
        /// 移除宏定义
        /// </summary>
        public static void Remove(string symbol)
        {
            var settings = DefineSymbolSettings.Instance;
            if (settings == null) return;

            var removedFromSettings = settings.Remove(symbol);
            if (removedFromSettings)
            {
                EditorUtility.SetDirty(settings);
            }

            RemoveFromPlayerSettings(symbol);
            SyncToPlayerSettings();
        }

        /// <summary>
        /// 检查宏定义是否存在
        /// </summary>
        public static bool Has(string symbol)
        {
            var settings = DefineSymbolSettings.Instance;
            return settings?.Has(symbol) ?? false;
        }

        /// <summary>
        /// 获取所有启用的宏
        /// </summary>
        public static List<string> GetAll()
        {
            var settings = DefineSymbolSettings.Instance;
            return settings?.GetEnabledSymbols() ?? new List<string>();
        }

        /// <summary>
        /// 同步设置到 PlayerSettings
        /// </summary>
        public static void SyncToPlayerSettings()
        {
            var settings = DefineSymbolSettings.Instance;
            if (settings == null) return;

            var target = EditorUserBuildSettings.selectedBuildTargetGroup;
            if (target == BuildTargetGroup.Unknown) return;

            var currentDefines = GetPlayerDefines(target);
            var managedSymbols = settings.GetEnabledSymbols();

            // 获取所有在设置中的符号（包括禁用的）
            var allManagedSymbols = new HashSet<string>();
            foreach (var d in settings.defines)
            {
                if (!string.IsNullOrEmpty(d.symbol))
                    allManagedSymbols.Add(d.symbol);
            }

            // 移除被管理但已禁用的符号
            var newDefines = currentDefines.Where(d => !allManagedSymbols.Contains(d) || managedSymbols.Contains(d)).ToList();

            // 添加启用的符号
            foreach (var symbol in managedSymbols)
            {
                if (!newDefines.Contains(symbol))
                    newDefines.Add(symbol);
            }

            // 检查是否有变化
            if (AreEqual(currentDefines, newDefines)) return;

            SetPlayerDefines(target, newDefines);
        }

        private static List<string> GetPlayerDefines(BuildTargetGroup target)
        {
            var definesStr = PlayerSettings.GetScriptingDefineSymbolsForGroup(target);
            return string.IsNullOrEmpty(definesStr)
                ? new List<string>()
                : definesStr.Split(';').Where(s => !string.IsNullOrEmpty(s)).ToList();
        }

        private static void SetPlayerDefines(BuildTargetGroup target, List<string> defines)
        {
            var definesStr = string.Join(";", defines);
            PlayerSettings.SetScriptingDefineSymbolsForGroup(target, definesStr);
        }

        private static void RemoveFromPlayerSettings(string symbol)
        {
            if (string.IsNullOrEmpty(symbol)) return;

            foreach (BuildTargetGroup target in System.Enum.GetValues(typeof(BuildTargetGroup)))
            {
                if (target == BuildTargetGroup.Unknown)
                    continue;

                try
                {
                    var currentDefines = GetPlayerDefines(target);
                    var newDefines = currentDefines.Where(d => d != symbol).ToList();
                    if (!AreEqual(currentDefines, newDefines))
                        SetPlayerDefines(target, newDefines);
                }
                catch
                {
                    // Some Unity versions expose enum values that are not valid define targets.
                }
            }
        }

        private static bool AreEqual(List<string> a, List<string> b)
        {
            if (a.Count != b.Count) return false;
            var setA = new HashSet<string>(a);
            var setB = new HashSet<string>(b);
            return setA.SetEquals(setB);
        }
    }
}
#endif
