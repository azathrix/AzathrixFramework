using Azathrix.Framework.Editor.Pipeline;
using UnityEditor;
using UnityEngine;

namespace Azathrix.Framework.Editor.Registry
{
    public static class RegistryUpdater
    {
        [InitializeOnLoadMethod]
        static void Initialize()
        {
            // 编辑器启动时延迟刷新，确保所有程序集都已加载
            EditorApplication.delayCall += () =>
            {
                // 如果正在编译，不执行扫描
                if (EditorApplication.isCompiling)
                    return;
                UpdateAllRegistries();
            };
        }

       // [MenuItem("Azathrix/注册表/刷新注册表")]
        public static void UpdateAllRegistries()
        {
            // 如果正在编译，不执行扫描
            if (EditorApplication.isCompiling)
            {
                Debug.LogWarning("[RegistryUpdater] 正在编译中，跳过扫描");
                return;
            }

            SystemRegistryScanner.Scan();
            PipelineRegistryScanner.ScanAll();

            AssetDatabase.SaveAssets();
        }
    } 
}
