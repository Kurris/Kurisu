---
applyTo: 'src/Kurisu.Extensions.SqlSugar/**'
description: 'Kurisu.Extensions.SqlSugar 使用说明'
---

---

# 注入与初始化

在 `ConfigureServices` 中通过扩展方法快速添加：

- 调用：
    - services.AddSqlSugar(dbType, configDb)
        - dbType: 指定数据库类型（例如 SqlSugar.DbType.SqlServer、MySql 等）。
        - configDb: 可选回调 (IServiceProvider, ISqlSugarClient) 用于在创建客户端之后做额外配置（例如追加 AOP 或自定义行为）。

示例：

```csharp
services.AddSqlSugar(DbType.SqlServer, (sp, db) => {
    // 可选：对 db 进行额外配置（比如注册自定义 AOP 或连接池策略）
});
```

如何在配置文件中设置（示例）:

```json
"SqlSugarOptions": {
"DefaultConnectionString": "Server=...;Database=...;User Id=...;Password=...;",
"EnableSqlLog": true,
"Timeout": 30,
"SlowSqlTime": 5
}
```

# 如何使用数据库操作

> 推荐在服务层注入 `IDbContext` 进行数据库操作

> 禁止直接使用 `ISqlSugarClient` 进行数据库操作,否则会导致租户,软删除等功能失效(重点)
> 禁止直接使用`Insertable<T>()`等方法进行数据库操作,否则会导致租户,软删除等功能失效(重点)
> 禁止直接使用`Updateable<T>()`等方法进行数据库操作,否则会导致租户,软删除等功能失效(重点)
> 禁止直接使用`Deleteable<T>()`等方法进行数据库操作,否则会导致租户,软删除等功能失效(重点)

## Select 查询数据

- 使用 `IDbContext` 提供的扩展方法 `IDbContextExtensions.Queryable<T>()` 方法进行查询,这会返回`ISugarQueryable<T>`对象,剩余的操作跟 SqlSugar 一致
- 支持常用的 Where, OrderBy, Select, ToListAsync 等方法
- 通过 Select 可以返回自定义 Dto 对象或者匿名对象,避免通过实体对象返回再进行转换

```csharp
var rooms = await dbContext.Queryable<Room>()
    .Where(r => r.Floor == 1)
    .OrderBy(r => r.Name)
    .Select(r => new RoomDto
    {
        Id = r.Id,
        Name = r.Name,
        Floor = r.Floor
    })
    .ToListAsync();
```

## Insert 插入数据

- 使用 `IDbContext` 提供的 `InsertAsync<T>(T entity)` / `InsertAsync<T>(List<T> entities)` 方法进行插入

```csharp
var newRoom = new Room
{
    Name = "新房间",
    Floor = 1
};
await dbContext.InsertAsync(newRoom);
```

## Update 更新数据

- 使用 `IDbContext` 提供的 `UpdateAsync<T>(T entity)` / `UpdateAsync<T>(List<T> entities)` 方法进行更新
- 优先先查询数据确保存在,然后再进行更新操作

```csharp
var room = await dbContext.Queryable<Room>()
    .Where(r => r.Id == roomId)
    .SingleAsync();

room.ThrowIfNull("房间不存在");
room.Name = newName;
await dbContext.UpdateAsync(room);
```

## Delete 删除数据

- 使用 `IDbContext` 提供的 `DeleteAsync<T>(T entity)` / `DeleteAsync<T>(List<T> entities)` 方法进行删除
- 对于实现了 `ISoftDeleted` 的实体，删除操作会自动进行软删除（将 `IsDeleted` 标记为 true）

```csharp
await dbContext.DeleteAsync(room);
```

## 事务操作

- 方法上定义 `[Transactional]` 特性以启用事务支持

```csharp
[Transactional]
public async Task CreateRoomAsync(RoomCreateReq req)
{
    var room = new Room
    {
        Name = req.Name,
        Floor = req.Floor
    };
    await _dbContext.InsertAsync(room);
    // 其他数据库操作
}
```

## 多库切换

- 方法上定义 `[Datasource("connectionName")]` 特性以切换到指定的数据库连接

```csharp
[Datasource("secondaryDb")]
public async Task UseSecondaryDbAsync()
{
    // 在此方法中使用 secondaryDb 连接
}
```

## 混合使用事务与多库切换

- 可以同时使用 `[Transactional]` 和 `[Datasource("connectionName")]` 特性
- 必须先定义 `[Datasource]`，然后定义 `[Transactional]`

```csharp
[Datasource("secondaryDb")]
[Transactional]
public async Task UseSecondaryDbWithTransactionAsync()
{
    // 在此方法中使用 secondaryDb 连接，并启用事务支持
}
```

## 忽略租户

- 方法上定义 `[IgnoreTenant]` 特性以忽略租户过滤
- 写入时候,如果实体继承了 `ITenantId`，那么则需要手动赋值,否则不需要手动处理`TenantId`字段,这是重点

```csharp
[IgnoreTenant]
public async Task<List<Room>> GetAllRoomsAsync()
{
    return await _dbContext.Queryable<Room>().ToListAsync();
}
```
