# Kurisu.AspNetCore 框架开发规范指南（.NET 8）

## ⚠️ 严格禁止的行为

| 类别   | 禁止项                                    |
|------|----------------------------------------|
| 类型系统 | 使用 `string?`、`T?` 等可空类型                |
| 控制器  | 返回 `IActionResult`、捕获异常、同步调用、`void`    |
| 服务层  | 引用 `Req/Resp`、不使用 DTO、不标注 `[DiInject]` |
| 数据映射 | 手动字段赋值                                 |
| 异常处理 | Controller 中返回错误（必须抛出异常）               |
| 实体   | 禁止新增或修改字段                              |

---

## 🎯 项目基础配置

### 核心约束

- **框架版本**：.NET 8
- **空值处理**：`<Nullable>disable</Nullable>`，**严禁使用 `string?`、`User?` 等可空类型**
- **依赖注入**：强制使用**主构造函数**注入
- **实体位置**：`Yaens.Hdms.Model` 项目
- **文档要求**：所有公开成员必须包含 XML 注释

---

## 📁 目录结构规范

### 模块化组织原则

按业务功能划分目录，相关实体统一管理

- 示例：房间管理涉及楼层、单元、楼幢、区域 → 统一归入 `Facilities` 模块
- 目录命名：以核心实体命名（如 `Room` → `Rooms`）

### 标准目录结构

```
{Module}/
├── Controllers/          # API 控制器
├── Models/
│   ├── Requests/         # 请求模型 (EntityActionReq)
│   └── Responses/        # 响应模型 (EntityActionResp)
├── Services/
│   ├── I{Entity}Service  # 服务接口
│   └── Impls/
│       └── {Entity}Service # 服务实现
├── DTOs/
│   ├── Commands/         # 命令 DTO (EntityActionCommand)
│   ├── Queries/          # 查询 DTO (EntityQuery)
│   └── Results/          # 结果 DTO (EntityResult)
└── Logicals/
    ├── Abstract{Entity}{Action}Logical # 抽象逻辑
    └── Default{Entity}{Action}Logical  # 默认实现
```

### 命名约定

| 组件   | 格式                                | 示例                          |
|------|-----------------------------------|-----------------------------|
| 控制器  | `{Entity}Controller`              | `RoomController`            |
| 请求类  | `{Entity}{Action}Req`             | `RoomCreateReq`             |
| 响应类  | `{Entity}{Action}Resp`            | `RoomCreateResp`            |
| 服务接口 | `I{Entity}Service`                | `IRoomService`              |
| 服务实现 | `{Entity}Service`                 | `RoomService`               |
| DTO  | `{Entity}{Action}Command`         | `RoomCreateCommand`         |
| 逻辑抽象 | `Abstract{Entity}{Action}Logical` | `AbstractRoomCreateLogical` |
| 逻辑实现 | `Default{Entity}{Action}Logical`  | `DefaultRoomCreateLogical`  |

---

## 🎛️ 控制器开发规范

### 基础配置

```csharp
[ApiController]
[Route("api/[controller]")]
public class RoomController(IRoomService roomService) : ControllerBase
{
    // 使用主构造函数注入
}
```

### 方法规范

- **命名**：`{Action}Async`（如 `CreateAsync`）
- **路由**：明确标注 HTTP 特性（`[HttpPost("save")]`）
- **参数绑定**：
    - GET 复杂对象：`[FromQuery]`
    - POST/PUT/PATCH 复杂对象：`[FromBody]`
    - 路由参数：直接作为方法参数

### 返回值约束（框架统一处理）

- **成功**：直接返回数据对象 → `Task<T>`
- **失败**：抛出异常（框架自动包装错误响应）必须使用 `UserFriendlyException`
- **无返回**：使用 `Task`
- **禁用**：`IActionResult`、`ActionResult<T>`、`void`、`async void`

**示例代码：**

```csharp
[HttpGet("list")]
public async Task<List<RoomResp>> ListAsync([FromQuery] RoomListReq req) { }

[HttpPost("create")]
public async Task<RoomCreateResp> CreateAsync([FromBody] RoomCreateReq req) { }
```

---

## ⚙️ 服务层开发规范

### 接口与实现

- **接口位置**：`Services/`
- **实现位置**：`Services/Impls/`
- **注入标记**：`[DiInject]`
- **方法命名**：`{Action}Async`
- **返回类型**：`Task<T>` 或 `Task`

### 数据传输限制

- **严禁**：引用 `Requests`/`Responses`
- **仅允许**：使用 `DTOs` 进行数据传输
- **CRUD 操作**：通过 DTO 映射到实体

**示例代码：**

```csharp
[DiInject]
public class RoomService : IRoomService
{
    public async Task<RoomCreateDto> CreateAsync(RoomCreateDto dto)
    {
        var entity = dto.Map<Room>();
        // 数据库操作
        return result.Map<RoomCreateDto>();
    }
}
```

---

## 📦 DTO 设计规范

### 传输范围

- **仅限**：Controller ↔ Service 层间传输
- **禁止**：在其他层使用

### 分类存储

- **Commands**：写操作 DTO（如 `RoomCreateCommand`）
- **Queries**：查询条件 DTO（如 `RoomQuery`）
- **Results**：查询结果 DTO（如 `RoomListResult`）

**示例代码：**

```csharp
public class RoomCreateCommand
{
    public string Name { get; set; }
    public int Floor { get; set; }
}
```

---

## 🔁 数据映射规范

### 推荐用法

- **创建新对象**：`req.Map<RoomCreateCommand>()`
- **更新对象**：`req.Map(room)`
- **忽略 null 字段**：`req.MapIgnoreNull(room)`
- **禁用**：手动字段赋值（易遗漏）

---

## 🧠 复杂逻辑处理规范

### 抽象类设计

- 定义 `Handle` 方法控制执行流程
- 将复杂逻辑拆分为虚方法供子类实现

### 实现类配置

- 标注 `[DiInject("Name")]` 进行命名注入
- 所有方法必须异步

**示例代码：**

```csharp
public abstract class AbstractRoomCreateLogical
{
    public async Task<RoomCreateDto> Handle(RoomCreateReq req)
    {
        await ValidateAsync(req);
        var room = await CreateRoomAsync(req);
        return MapToDto(room);
    }

    protected abstract Task ValidateAsync(RoomCreateReq req);
    protected abstract Task<Room> CreateRoomAsync(RoomCreateReq req);
    protected abstract RoomCreateDto MapToDto(Room room);
}

[DiInject("DefaultRoomCreateLogical")]
public class DefaultRoomCreateLogical : AbstractRoomCreateLogical
{
    // 实现具体逻辑
}
```

---

## 🛠️ 实战最佳实践

### 参数验证与部分更新

- **PATCH 操作**：使用 `GetNotNullPropertyNames()` 获取非空字段
- **仅更新非空字段**，避免 null 覆盖数据库

### 分页处理策略

- 检查目标分页类型是否含只读属性
- 选择忽略只读属性或手动重建分页对象

### 日志与异常

- **Controller**：使用 `[Log("操作描述")]` 记录操作
- **异常处理**：在 Service 层抛出，Controller 不捕获

---

## 📝 注释与文档要求

- **XML 注释**：所有公开类、方法、属性必须包含
- **参数说明**：包含含义与示例值
- **方法摘要**：简明扼要描述功能
