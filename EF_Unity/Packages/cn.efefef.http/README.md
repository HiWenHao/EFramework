# EF.Http

HTTP 请求门面：可替换的发送 / 缓存 / 重试 / 设置接口，门面只做编排，生命周期与持久化边界纪律化。

## 门面

`EasyFramework.Managers.Http.HttpManager`（`[Manager]`，`MonoSingleton`）

```csharp
string text = await HttpManager.Instance.GetStringAsync(url);
T dto = await HttpManager.Instance.GetAsync<T>(url);
T2 result = await HttpManager.Instance.PostJsonAsync<T1, T2>(url, payload);
Texture2D tex = await HttpManager.Instance.GetTextureAsync(url);
```

- REST：`GetStringAsync` / `GetAsync<T>` / `GetTextureAsync` / `GetAssetBundleAsync` /
  `PostJsonAsync` / `PostMultipartAsync` / `UploadFileAsync` / `PutAsync` / `DeleteAsync` / 下载。
- 四类服务可替换：`SetSender(IHttpSender)` / `SetCache(IHttpCache)` /
  `SetRetryPolicy(IHttpRetryPolicy)` / `SetSettings(IHttpSettings)`；`Configure(HttpServices)` 整体注入。
- 缓存为可选装饰器，包内不预置实现；业务可注入 `MemoryHttpCache`。
- `OpenDebug` 控制警告日志。

依赖：`cn.efefef.core`、`com.unity.nuget.newtonsoft-json`。
