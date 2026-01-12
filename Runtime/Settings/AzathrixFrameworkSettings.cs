using System.Collections.Generic;
using Azathrix.Framework.Core.Configs;
using UnityEngine;

namespace Azathrix.Framework.Settings
{
    /// <summary>
    /// 扫描模式
    /// </summary>
    public enum ScanMode
    {
        /// <summary>
        /// 扫描所有程序集（排除系统程序集）
        /// </summary>
        All,

        /// <summary>
        /// 只扫描指定的程序集
        /// </summary>
        Specified
    }

    /// <summary>
    /// 系统信息输出级别
    /// </summary>
    public enum SystemInfoLevel
    {
        None,
        Simple,
        Detailed
    }

    /// <summary>
    /// 框架配置
    /// </summary>
    [SettingsPath("AzathrixFrameworkSettings")]
    [ShowSetting("框架设置")]
    public class AzathrixFrameworkSettings : SettingsBase<AzathrixFrameworkSettings>
    {
        [Header("项目配置")]
        [Tooltip("项目ID（用于资源路径等）")]
        public string projectId = "NewGame";

        [Header("版本配置")]
        [Tooltip("版本格式")]
        public string versionFormat = "{major}.{minor}.{patch}";
        public int majorVersion = 1;
        public int minorVersion = 0;
        public int patchVersion = 0;
        public int buildNumber = 1;

        /// <summary>
        /// 获取游戏版本
        /// </summary>
        public string Version => versionFormat
            .Replace("{major}", majorVersion.ToString())
            .Replace("{minor}", minorVersion.ToString())
            .Replace("{patch}", patchVersion.ToString())
            .Replace("{build}", buildNumber.ToString());

        /// <summary>
        /// 获取完整版本（含 build number）
        /// </summary>
        public string FullVersion => $"{Version}.{buildNumber}";

        [Header("扫描配置")] [Tooltip("扫描模式")] public ScanMode scanMode = ScanMode.All;

        [Tooltip("要扫描的程序集名称（ScanMode.Specified 时生效）")]
        public List<string> assemblyNames = new();

        [Tooltip("排除的程序集前缀")] public List<string> excludeAssemblyPrefixes = new()
        {
            "System",
            "Microsoft",
            "Unity",
            "mscorlib",
            "netstandard",
            "Mono",
            "nunit"
        };

        [Header("Runtime 配置")] [Tooltip("是否启用性能统计")]
        public bool enableProfiling;

        [Header("初始化配置")] [Tooltip("自动初始化框架")]
        public bool autoInitialize = true;

        [Header("日志配置")]
        [Tooltip("系统信息输出级别")]
        public SystemInfoLevel systemInfoLevel = SystemInfoLevel.Simple;

        [Tooltip("是否显示编辑器管线调试日志")]
        public bool debugEditorPipeline;


        /// <summary>
        /// 转换为 ScannerConfig
        /// </summary>
        public ScannerConfig ToScannerConfig()
        {
            var config = new ScannerConfig
            {
                ExcludeAssemblyPrefixes = new List<string>(excludeAssemblyPrefixes)
            };

            if (scanMode == ScanMode.Specified)
            {
                config.AssemblyPrefixes = new List<string>(assemblyNames);
            }

            return config;
        }

        /// <summary>
        /// 转换为 RuntimeConfig
        /// </summary>
        public RuntimeConfig ToRuntimeConfig()
        {
            return new RuntimeConfig
            {
                EnableProfiling = enableProfiling
            };
        }

#if UNITY_EDITOR
        /// <summary>
        /// 保存设置
        /// </summary>
        public void Save()
        {
            UnityEditor.EditorUtility.SetDirty(this);
            UnityEditor.AssetDatabase.SaveAssetIfDirty(this);
        }
#endif

    }
}
