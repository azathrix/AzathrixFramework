# 安装

## 方式一：Package Manager 添加 Scope（推荐）

1. 打开 `Edit > Project Settings > Package Manager`
2. 在 `Scoped Registries` 中添加：
   - **Name**: `Azathrix`
   - **URL**: `https://registry.npmjs.org`
   - **Scope(s)**: `com.azathrix`
3. 点击 `Save`
4. 打开 `Window > Package Manager`
5. 切换到 `My Registries`
6. 找到 `Azathrix Framework` 并安装

## 方式二：Git URL

1. 打开 `Window > Package Manager`
2. 点击 `+` > `Add package from git URL...`
3. 输入：`https://github.com/AzathrixDev/com.azathrix.framework.git#latest`

::: warning 注意
Git 方式无法自动解析依赖，需要先手动安装 [UniTask](https://github.com/Cysharp/UniTask)
:::

## 方式三：npm 命令

在项目的 `Packages` 目录下执行：

```bash
npm install com.azathrix.framework
```

## 依赖

| 依赖 | 版本 | 说明 |
|------|------|------|
| com.azathrix.unitask | 2.5.10+ | 异步任务库（自动安装） |

## 验证安装

安装完成后，打开 `Azathrix > Settings` 菜单，如果能看到框架设置面板，说明安装成功。

## 配置

在 `Project Settings > Azathrix > 框架设置` 中可以配置：

- **扫描模式** - All（全部）或 Specified（指定程序集）
- **自动初始化** - 进入 Play 模式时自动启动框架
- **日志级别** - 控制日志输出详细程度
