---
applyTo: '**'
description: 'Kurisu.RemoteCall 使用约定与示例（面向开发者或 AI 模型的简洁 prompt 版）。'
---

# Kurisu.RemoteCall — 快速使用指南（用于 prompt / 开发快速参考）

要点摘要：
- 用法：在接口上标注 `[EnableRemoteClient(name, baseUrl, PolicyHandler = typeof(...))]` 开启远程调用代理。代理通过 `IHttpClientFactory` 获取命名 HttpClient。
- URL 占位符：支持 `${key}`，按运行时提供的 `IConfiguration` 值替换。
- Query 与 Route：支持 `[RequestQuery]`（可展开复杂对象）与 `[RequestRoute]`（替换 URL 中 `{name}` 占位符）。
- 请求体：默认发送 JSON；支持 `asUrlencodedFormat` 将参数组织为 urlencoded 字符串 / 表单；可用 `[RequestContent]` 指定自定义 `HttpContent` 生成器。
- 授权：可在接口/方法上使用 `[RequestAuthorize]` 指定鉴权 handler（默认 handler 从 HttpContext 的 Authorization header 获取 token —— 若 HttpContext/header 缺失，默认 handler 会抛异常）。
- 参数验证：默认只对引用/复杂类型做 DataAnnotation 验证（简单类型和 null 会被跳过）。
- DI 注意：如果在 `EnableRemoteClient` 指定 `PolicyHandler`，该类型会通过无参构造创建并调用其配置方法，因此需有无参构造；如需 DI 注入式构造，请先在容器中注册并在 ConfigureServices 时解析调用。

短示例（用于 prompt 或快速复制）：

- GET（简单参数）：

```csharp
[EnableRemoteClient("demo-client", "https://api.example.com")]
public interface IExampleApi
{
    [Get("items/list")]
    Task<string> GetItemsAsync(string keyword, int page);
}
```

请求示例（URL 生成）： GET https://api.example.com/items/list?keyword=...&page=...

- GET（复杂对象作为 query）：

```csharp
[Get("items/search")]
Task<string> SearchAsync([RequestQuery] SearchFilter filter);
```

说明：当参数被标注为 `[RequestQuery]` 且为复杂对象时，该对象会被展开为多个 query 键值对（属性 -> key），注意展开后的键/值可能不再额外编码，必要时对产生的键/值进行编码或使用简单参数。

- POST（发送 JSON）：

```csharp
[Post("items/create")]
Task<ResultDto> CreateAsync([RequestBody] CreateItemDto dto);
```

说明：默认会把 `dto` 序列化为 JSON 并作为请求体发送（Content-Type 默认为 application/json）。

- POST（表单/URL-encoded）：

```csharp
[Post("items/create", contentType: "application/x-www-form-urlencoded", asUrlencodedFormat: true)]
Task<string> CreateFormAsync(string name, int qty);
```

说明：当 `asUrlencodedFormat=true` 且 `contentType` 为 `application/x-www-form-urlencoded`，框架会使用标准表单编码（FormUrlEncodedContent）。对于单个 string 原始 `"key=val&..."` 的特殊情况，框架会解析并重编码；若仅传单个 string 且同时要求 FormUrlEncodedContent，避免直接传原始字符串（可能会抛异常）。

- 自定义请求内容（multipart/流等）：

```csharp
[Post("upload")]
[RequestContent(typeof(MyUploadContentHandler))]
Task<string> UploadAsync(FileUploadModel file);

public class MyUploadContentHandler : IRemoteCallContentHandler
{
    public HttpContent UploadAsync(FileUploadModel file)
    {
        var content = new MultipartFormDataContent();
        // ... add file/fields ...
        return content;
    }
}
```

说明：自定义 handler 的方法名应与接口方法名一致，参数列表需匹配接口方法签名，方法需返回 `HttpContent`。框架会在 DI 容器中解析 handler 并调用该方法来获取请求体。

最佳实践（简短）：
- 明确 `BaseUrl` 与模板占位符，确认运行时 `IConfiguration` 中包含需要的键。
- 对复杂 query 对象，显式控制生成的键/值编码以避免后端解析差异。
- 对鉴权与无 HttpContext 场景，考虑提供自定义 `IRemoteCallAuthTokenHandler` 以决定是否跳过或提供 token。
- 若需要 Mock HttpMessageHandler 或重试/熔断策略，使用 `PolicyHandler`（注意其构造方式），或在 DI 中预先注册并在 ConfigureServices 中解析配置。

## 授权、自定义 Header 与 自定义响应解析器（简明指南）

授权（Authorization）:
- 在接口或方法上添加 `[RequestAuthorize]`（或 `[RequestAuthorize(typeof(YourHandler))]`）启用鉴权。
- 默认 `RequestAuthorize` 使用内置 handler 从 `HttpContext` 的 `Authorization` header 获取 token；默认 handler 在没有 `HttpContext` 或缺少 header 时会抛异常。若希望在后台任务中跳过或自定义 token 来源，请实现 `IRemoteCallAuthTokenHandler` 并在 Attribute 中指定。

示例：接口级鉴权（使用默认 handler）

```csharp
[EnableRemoteClient("api", "https://api.example.com")]
[RequestAuthorize]
public interface IProtectedApi
{
    [Get("secure/data")]
    Task<string> GetSecureAsync();
}
```

自定义 Header：
- 可以在接口或方法上使用 `[RequestHeader("Name","Value")]` 添加静态 header；也可以使用 `[RequestHeader(typeof(MyHeaderHandler))]` 指定一个实现 `IRemoteCallHeaderHandler` 的类型来动态生成 header（框架会从 DI 中解析该 handler 并调用 `GetHeadersAsync`）。

静态 Header 示例：

```csharp
[RequestHeader("X-API-KEY", "abcd1234")]
Task<string> GetWithKeyAsync();
```

动态 Header 示例（handler）：

```csharp
public class MyHeaderHandler : IRemoteCallHeaderHandler
{
    public Task<Dictionary<string, string>> GetHeadersAsync()
    {
        return Task.FromResult(new Dictionary<string, string>
        {
            ["X-Request-Id"] = Guid.NewGuid().ToString(),
        });
    }
}

[RequestHeader(typeof(MyHeaderHandler))]
Task<string> GetWithDynamicHeaderAsync();
```

自定义响应解析器（ResponseResult）：
- 如果后端返回统一包装（例如 { code,msg,data }），可在接口或方法上使用 `[ResponseResult(typeof(MyResponseHandler))]` 指定自定义解析器。该 handler 需要实现 `IRemoteCallResponseResultHandler`，并在其 `Handle<TResult>` 方法中根据 statusCode 和 responseBody 返回最终的 TResult 或抛出相应异常。

示例：自定义响应解析器

```csharp
public class MyResponseHandler : IRemoteCallResponseResultHandler
{
    public TResult Handle<TResult>(HttpStatusCode statusCode, string responseBody)
    {
        // 假设后端返回 { code: 0, message: "ok", data: ... }
        var wrapper = JsonConvert.DeserializeObject<ResponseWrapper>(responseBody);
        if (wrapper == null) throw new HttpRequestException("Empty response");
        if (wrapper.code != 0) throw new Exception(wrapper.message);
        return JsonConvert.DeserializeObject<TResult>(wrapper.data.ToString());
    }
}

[ResponseResult(typeof(MyResponseHandler))]
Task<MyDto> GetWrappedAsync();
```

注意：自定义的 header handler 与响应解析器会在 DI 中被解析，请确保这些类型已注册或可通过构造解析。

+## 禁用日志（RequestDisableLogAttribute）
+
+- 作用：在接口或单个方法上标注 `[RequestDisableLog]` 可使框架的请求拦截器跳过该调用的日志输出（包含请求方法、URL、请求体以及响应体或异常信息）。常用于避免在日志中记录敏感数据或降低高频调用的日志噪音。
+- 使用位置：可标注在接口或接口方法上；方法级特性会覆盖接口级特性（方法上标注优先）。
+
+示例：
+
+```csharp
+[EnableRemoteClient("api","https://api.example.com")]
+public interface IApi
+{
+    [RequestDisableLog]
+    Task<string> GetSensitiveAsync();
+}
+```
+
+注意：该特性仅影响 Kurisu.RemoteCall 的拦截器级日志输出，不会影响其他日志中间件或外部组件的日志策略。
