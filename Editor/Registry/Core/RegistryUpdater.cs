using UnityEditor;
using UnityEngine;

namespace Azathrix.Framework.Editor.Registry
{
    public static class RegistryUpdater
    {
        [InitializeOnLoadMethod]
        static void Initialize()
        {
            // 编辑器启动时延迟刷新
            EditorApplication.delayCall += UpdateAllRegistries;
        }

        [MenuItem("Azathrix/注册表/刷新注册表")]
        public static void UpdateAllRegistries()
        {
            // Debug.Log("[RegistryUpdater] 开始更新所有注册表...");

            SystemRegistryScanner.Scan();
            PhaseRegistryScanner.Scan();
            HookRegistryScanner.Scan();

            AssetDatabase.SaveAssets();
            // Debug.Log("[RegistryUpdater] 注册表更新完成");
        }
    }
}
