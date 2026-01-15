using System;
using System.Collections.Generic;
using Azathrix.Framework.Settings;
using UnityEngine;

namespace Azathrix.Framework
{
    /// <summary>
    /// 宏定义设置
    /// </summary>
    [SettingsPath("DefineSymbolSettings")]
    [ShowSetting("宏定义设置")]
    public class DefineSymbolSettings : SettingsBase<DefineSymbolSettings>
    {
        [Serializable]
        public class DefineEntry
        {
            public string symbol;
            public string source;  // 来源（如 "Azcel", "EnvInstaller" 等）
            public bool enabled = true;
        }

        [SerializeField]
        public List<DefineEntry> defines = new();

        /// <summary>
        /// 添加宏定义
        /// </summary>
        public bool Add(string symbol, string source = null)
        {
            if (string.IsNullOrEmpty(symbol)) return false;
            if (defines.Exists(d => d.symbol == symbol)) return false;

            defines.Add(new DefineEntry
            {
                symbol = symbol,
                source = source ?? "Manual",
                enabled = true
            });
            return true;
        }

        /// <summary>
        /// 移除宏定义
        /// </summary>
        public bool Remove(string symbol)
        {
            return defines.RemoveAll(d => d.symbol == symbol) > 0;
        }

        /// <summary>
        /// 检查宏是否存在
        /// </summary>
        public bool Has(string symbol)
        {
            return defines.Exists(d => d.symbol == symbol && d.enabled);
        }

        /// <summary>
        /// 获取所有启用的宏
        /// </summary>
        public List<string> GetEnabledSymbols()
        {
            var result = new List<string>();
            foreach (var d in defines)
            {
                if (d.enabled && !string.IsNullOrEmpty(d.symbol))
                    result.Add(d.symbol);
            }
            return result;
        }

#if UNITY_EDITOR
        public static event Action OnSettingsChanged;

        private void OnValidate()
        {
            UnityEditor.EditorApplication.delayCall += () => OnSettingsChanged?.Invoke();
        }
#endif
    }
}
