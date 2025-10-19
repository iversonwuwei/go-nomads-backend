# gRPC 代码重新生成指南

本项目使用**预生成的 gRPC 代码**，而不是在容器构建时动态生成。这样可以避免 ARM64 架构上的 protoc 兼容性问题。

## 📁 目录结构

```
src/Shared/Shared/
├── Protos/                    # Proto 源文件（.proto）
│   ├── user.proto
│   └── product.proto
├── Generated/Protos/          # 预生成的 C# 代码（提交到 Git）
│   ├── User.cs
│   ├── UserGrpc.cs
│   ├── Product.cs
│   └── ProductGrpc.cs
└── Shared.csproj              # 项目文件
```

## 🔄 何时需要重新生成

当你修改了 `.proto` 文件后，需要重新生成 gRPC 代码：

1. 添加新的 RPC 方法
2. 修改消息定义
3. 添加新的 .proto 文件

## 🛠️ 重新生成步骤

### 步骤 1: 启用 Grpc.Tools

编辑 `src/Shared/Shared/Shared.csproj`，取消注释 Grpc.Tools：

```xml
<ItemGroup>
  <PackageReference Include="Google.Protobuf" Version="3.32.1" />
  <PackageReference Include="Grpc.Net.Client" Version="2.71.0" />
  <!-- 取消下面的注释 -->
  <PackageReference Include="Grpc.Tools" Version="2.72.0">
    <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
    <PrivateAssets>all</PrivateAssets>
  </PackageReference>
</ItemGroup>

<ItemGroup>
  <!-- 取消下面的注释 -->
  <Protobuf Include="Protos\user.proto" GrpcServices="Both" />
  <Protobuf Include="Protos\product.proto" GrpcServices="Both" />
</ItemGroup>
```

### 步骤 2: 构建项目

```bash
cd src/Shared/Shared
dotnet clean
dotnet build
```

### 步骤 3: 复制生成的文件

```bash
# 复制新生成的文件到 Generated 目录
cp obj/Debug/net9.0/Protos/*.cs Generated/Protos/
```

### 步骤 4: 禁用 Grpc.Tools

再次编辑 `Shared.csproj`，注释掉 Grpc.Tools：

```xml
<!-- Grpc.Tools 仅在需要重新生成 proto 时使用 -->
<!--
<PackageReference Include="Grpc.Tools" Version="2.72.0">
  <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
  <PrivateAssets>all</PrivateAssets>
</PackageReference>
-->
```

### 步骤 5: 验证

```bash
dotnet clean
dotnet build
```

### 步骤 6: 提交到 Git

```bash
git add src/Shared/Shared/Generated/Protos/*.cs
git add src/Shared/Shared/Protos/*.proto
git commit -m "chore: regenerate gRPC code from updated proto files"
```

## 📦 一键脚本（推荐）

创建 `regenerate-grpc.sh` 脚本自动化上述步骤：

```bash
#!/bin/bash

echo "🔄 重新生成 gRPC 代码..."

# 1. 启用 Grpc.Tools
sed -i.bak 's/<!--\s*<PackageReference Include="Grpc.Tools"/<PackageReference Include="Grpc.Tools"/' src/Shared/Shared/Shared.csproj
sed -i.bak 's/<\/PackageReference>\s*-->/<\/PackageReference>/' src/Shared/Shared/Shared.csproj
sed -i.bak 's/<!--\s*<ItemGroup>\s*<Protobuf/<ItemGroup><Protobuf/' src/Shared/Shared/Shared.csproj

# 2. 构建
cd src/Shared/Shared
dotnet clean
dotnet build

# 3. 复制生成的文件
cp obj/Debug/net9.0/Protos/*.cs Generated/Protos/

# 4. 禁用 Grpc.Tools
mv Shared.csproj.bak Shared.csproj

# 5. 验证
dotnet clean
dotnet build

echo "✅ gRPC 代码重新生成完成！"
echo "请检查 Generated/Protos/ 目录并提交更改"
```

## ⚠️ 注意事项

1. **不要手动编辑** `Generated/Protos/` 中的文件，它们会在重新生成时被覆盖
2. **始终提交** `Generated/Protos/` 中的文件到 Git
3. **容器构建时不需要** Grpc.Tools，使用预生成的代码
4. 如果遇到命名冲突，检查 `.proto` 文件的 `package` 和 `option csharp_namespace` 设置

## 🚀 优势

✅ **ARM64 兼容** - 无需 x64 模拟器
✅ **构建速度快** - 容器构建时跳过 protoc 编译
✅ **可预测性** - 生成的代码版本可控
✅ **调试友好** - 可以直接查看和调试生成的代码

## 🔗 相关文档

- [gRPC for .NET](https://grpc.io/docs/languages/csharp/)
- [Protocol Buffers](https://developers.google.com/protocol-buffers)
- [Grpc.Tools NuGet Package](https://www.nuget.org/packages/Grpc.Tools/)
