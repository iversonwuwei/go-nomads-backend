# 构建阶段
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# 复制项目文件
COPY ["src/Services/MessageService/API/MessageService.csproj", "API/"]
COPY ["src/Services/MessageService/Application/MessageService.Application.csproj", "Application/"]
COPY ["src/Services/MessageService/Infrastructure/MessageService.Infrastructure.csproj", "Infrastructure/"]
COPY ["src/Services/MessageService/Domain/MessageService.Domain.csproj", "Domain/"]

# 还原依赖
RUN dotnet restore "API/MessageService.csproj"

# 复制所有源代码
COPY src/Services/MessageService/ .

# 构建应用
WORKDIR "/src/API"
RUN dotnet build "MessageService.csproj" -c Release -o /app/build

# 发布阶段
FROM build AS publish
RUN dotnet publish "MessageService.csproj" -c Release -o /app/publish /p:UseAppHost=false

# 运行时阶段
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

# 安装 curl（用于健康检查）
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

EXPOSE 5005

COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "MessageService.dll"]
