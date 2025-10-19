# 🔧 Shared 项目配置说明

## 项目类型变更

### 变更原因
为了支持 Consul 自动注册扩展方法 (`ConsulServiceRegistration.cs`)，需要引用 ASP.NET Core 相关类型：
- `WebApplication`
- `IServer`
- `IServerAddressesFeature`
- 等 ASP.NET Core 基础设施类型

### 解决方案
将 Shared 项目从普通类库改为 **Web 类库**：

```xml
<!-- 之前 -->
<Project Sdk="Microsoft.NET.Sdk">

<!-- 现在 -->
<Project Sdk="Microsoft.NET.Sdk.Web">
  <PropertyGroup>
    <OutputType>Library</OutputType>
    <IsPackable>true</IsPackable>
  </PropertyGroup>
</Project>
```

## 优势

### ✅ 自动引用 ASP.NET Core 包
使用 `Microsoft.NET.Sdk.Web` 会自动引用：
- `Microsoft.AspNetCore.App` 框架
- 所有 ASP.NET Core 基础设施类型
- 无需手动添加 NuGet 包

### ✅ 仍然是类库
通过设置：
```xml
<OutputType>Library</OutputType>
<IsPackable>true</IsPackable>
```
保持项目输出为类库（.dll），可以被其他项目引用。

### ✅ 向后兼容
对现有代码无影响：
- gRPC 相关代码正常工作
- Proto 生成文件正常使用
- 其他服务引用 Shared 无需修改

## 包含的功能

### 1. gRPC 支持
- `Google.Protobuf`
- `Grpc.Net.Client`
- 预生成的 gRPC 代码

### 2. Consul 自动注册
- `ConsulServiceRegistration.cs` 扩展方法
- 服务启动时自动注册
- 服务关闭时自动注销

### 3. 共享模型
- `Models/` 目录（如果需要）
- 其他共享代码

## 使用方式

### 在服务中引用
```xml
<ItemGroup>
  <ProjectReference Include="../../../Shared/Shared/Shared.csproj" />
</ItemGroup>
```

### 使用 Consul 注册
```csharp
using Shared.Extensions;

var app = builder.Build();

// ... 配置管道 ...

await app.RegisterWithConsulAsync();
app.Run();
```

## 注意事项

1. **不会创建可执行文件**
   - `OutputType=Library` 确保输出为 .dll
   - 不能直接运行，只能被引用

2. **包大小增加**
   - 引用了完整的 ASP.NET Core 框架
   - 但对最终发布的服务没有额外影响（框架共享）

3. **编译顺序**
   - Shared 项目必须先编译
   - 其他服务依赖 Shared

## 验证

```bash
# 编译 Shared 项目
cd src/Shared/Shared
dotnet build

# 应该看到：
# ✅ 已成功生成
# ✅ 0 个警告
# ✅ 0 个错误
```

---

**最后更新：** 2025-10-19
