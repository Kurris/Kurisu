---
description: "递归检查并同步所有 Kurisu.* 相关依赖包的版本，确保基础包升级后所有直接或间接引用的项目都同步升级 PackageReference，避免类型不兼容。适用于 Abstractions/Extensions/Aspect.Abstractions 升级场景。"
name: "Kurisu 依赖升级同步检查"
---

# Kurisu 依赖升级同步 Skill

## 适用场景

- 当 Kurisu.*.Abstractions、Kurisu.Extensions.*、Kurisu.Aspect.Abstractions 等基础包升级后，需统一同步所有引用链上的依赖版本。

- 适用于多项目/多层依赖的 .NET 解决方案。

## 项目依赖关系（完整列表）

升级任意包时，必须同步升级以下被直接影响的项目：

| 包名 | 直接依赖方（需同步升级的项目） |
|------|-------------------------------|
| `Kurisu.Aspect.Abstractions` | `Kurisu.Aspect`、`Kurisu.AspNetCore.Abstractions` |
| `Kurisu.AspNetCore.Abstractions` | `Kurisu.Extensions.Cache`、`Kurisu.Extensions.ContextAccessor`、`Kurisu.Extensions.SqlSugar`、`Kurisu.AspNetCore` |
| `Kurisu.Extensions.ContextAccessor` | `Kurisu.Extensions.SqlSugar` |
| `Kurisu.Extensions.Cache` | `Kurisu.AspNetCore` |
| `Kurisu.Extensions.SqlSugar` | `Kurisu.Extensions.EventBus` |
| `Kurisu.Aspect` | `Kurisu.AspNetCore` |

> **间接依赖**：如 `Kurisu.Aspect.Abstractions` 升级，除了直接影响 `Kurisu.Aspect` 和 `Kurisu.AspNetCore.Abstractions`，还需递归检查 `Kurisu.AspNetCore.Abstractions` 的所有依赖方（`Kurisu.Extensions.Cache`、`Kurisu.Extensions.ContextAccessor`、`Kurisu.Extensions.SqlSugar`、`Kurisu.AspNetCore`），以及这些依赖方各自的依赖方。即**全链路递归同步**。

## 流程

### 0. 环境变量检查（第一步，必须）

触发 skill 后**首先**检查以下环境变量是否存在，任一缺失则终止并报告：

| 变量名 | 用途 | 检查方式 |
|--------|------|----------|
| `NUGET_SERVER_URL` | 私有 NuGet 源地址 | 检查是否已设置 |
| `NUGET_API_KEY` | 私有 NuGet 源 API 密钥 | 检查是否已设置 |

> **敏感信息保护**：检查时只报告「已设置」或「未设置」，**绝不输出**环境变量的实际值。

检查示例：

```bash
[ -n "$NUGET_SERVER_URL" ] && echo "NUGET_SERVER_URL: 已设置" || echo "NUGET_SERVER_URL: 未设置"
[ -n "$NUGET_API_KEY" ] && echo "NUGET_API_KEY: 已设置" || echo "NUGET_API_KEY: 未设置"
```

环境变量导出模板（由开发者在本地 shell 配置中填写）：

```bash
export NUGET_SERVER_URL="https://your-nuget-server/v3/index.json"
export NUGET_API_KEY="your-nuget-api-key"
```

### 1. 确定升级包与版本类型

- 明确本次升级的基础包（如 `Kurisu.AspNetCore.Abstractions`、`Kurisu.Extensions.ContextAccessor` 等）。
- 指定升级类型：`patch` / `minor` / `major`（语义化版本）。

### 2. 检查 build 配置

- 确认目标项目的 `.csproj` 已配置 `<GeneratePackageOnBuild>true</GeneratePackageOnBuild>`，确保 Release build 后自动生成 `.nupkg` 文件于相对路径 `bin/Release/`。

### 3. 升级变更包自身版本

- 修改目标项目的 `<Version>` 字段（如 `0.1.5` → `0.1.6`），保存。

### 4. 构建变更包

- 执行 `dotnet build -c Release`，生成 `.nupkg`。

### 5. 推送变更包到私有 NuGet

- 直接使用相对路径推送（`bin/Release`）：

```bash
# 示例：包名为 Kurisu.Extensions.ContextAccessor.0.1.6.nupkg
cd src/Kurisu.Extensions.ContextAccessor/bin/Release

dotnet nuget push Kurisu.Extensions.ContextAccessor.0.1.6.nupkg \
  --source "$NUGET_SERVER_URL" \
  --api-key "$NUGET_API_KEY" \
  --skip-duplicate
```

### 6. 递归升级依赖链

- 遍历所有 `.csproj` 文件，查找直接或间接引用该包的项目。
- 按**从底向上**顺序处理依赖链。每处理一个项目，需**同时**完成两项升级：
  a. **升级引用的包版本**：将项目中指向已升级包的 `<PackageReference>` 版本号更新为最新
  b. **升级自身 Version**：提升本项目 `<Version>` 字段（如 `0.2.5` → `0.2.6`）
- 两项升级完成后：
  c. `dotnet build -c Release` 生成 `.nupkg`
  d. 推送到私有 NuGet（同步骤 5）
  e. `dotnet test` 验证

### 7. 最终验证

- 全部升级完成后，执行 `dotnet build -c Release` 和 `dotnet test`，确保类型兼容且功能无回归。

## 完成标准

- 变更包及所有直接/间接引用其的依赖项目，PackageReference 版本一致且为最新。
- 所有包均已推送到私有 NuGet 源。
- 构建、测试全部通过。

## 示例 prompt

- "Kurisu.AspNetCore.Abstractions 发布新版本，patch 升级并递归同步所有依赖链"
- "Kurisu.Extensions.ContextAccessor minor 升级后，帮我同步所有相关项目的依赖版本并推送"