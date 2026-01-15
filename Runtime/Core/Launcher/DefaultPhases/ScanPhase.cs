using System.Diagnostics;
using Azathrix.Framework.Tools;
using Cysharp.Threading.Tasks;

namespace Azathrix.Framework.Core.Launcher.DefaultPhases
{
    /// <summary>
    /// Scan阶段 - 扫描系统类型
    /// </summary>
    public class ScanPhase : IScanPhase
    {
        public string Id => "Scan";
        public int Order => 200;

        public async UniTask ExecuteAsync(LauncherContext context)
        {
            Log.Separator("Scan 阶段");
            Log.Info("[Scan] 开始扫描系统类型...");

            var scanner = new SystemScanner(AzathrixFramework.Logger);
            var watch = Stopwatch.StartNew();
            var scannedTypes = await scanner.ScanAsync();
            watch.Stop();

            Log.Info($"[Scan] 完成，发现 {scannedTypes.Length} 个系统，耗时: {watch.Elapsed.TotalMilliseconds:F2}ms");

            context.ScannedSystemTypes = scannedTypes;
        }
    }
}
