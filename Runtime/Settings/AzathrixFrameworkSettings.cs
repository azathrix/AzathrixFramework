using UnityEngine;

namespace Azathrix.Framework.Settings
{
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

        [Header("初始化配置")]
        [Tooltip("自动初始化框架")]
        public bool autoInitialize = true;

        [Header("日志配置")]
        [Tooltip("系统信息输出级别")]
        public SystemInfoLevel systemInfoLevel = SystemInfoLevel.Simple;

        [Tooltip("是否显示编辑器管线调试日志")]
        public bool debugEditorPipeline;

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
