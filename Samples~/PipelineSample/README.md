# 管线示例

本示例走“扫描 → 注册表 → 运行时管线”的完整流程：
- 示例管线 `SamplePipeline` 通过 `PipelineRegistry` 扫描注册
- 启动管线的 `IStartPhase` 后置钩子触发执行示例管线
- 验证全局钩子与类型钩子（具体类/基类/接口）的匹配与顺序

步骤：
1. 在 Package Manager 导入此 Sample。
2. 建议确保框架尚未启动（例如关闭 `autoInitialize`，或重新进入 Play）。
3. 通过菜单 `Azathrix/注册表/扫描管线` 扫描一次（生成注册表数据）。
4. 点击 Play（或手动调用 `AzathrixFramework.StartupAsync()`）。
5. 在 Console 查看带有 “[PipelineSample]” 前缀的日志。

说明：
- Sample 不需要挂 Mono 组件，启动管线的 `Start` 阶段后置钩子会触发示例管线。
- 可将 `SamplePipelineRuntime.AutoRunOnStartup` 设为 `false`，并在启动前调用 `SamplePipelineRuntime.RequestRun()` 来手动触发。
