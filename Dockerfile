# Build stage
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copy solution and project files
COPY ["LiaXP.sln", "./"]
COPY ["src/LiaXP.Domain/LiaXP.Domain.csproj", "src/LiaXP.Domain/"]
COPY ["src/LiaXP.Application/LiaXP.Application.csproj", "src/LiaXP.Application/"]
COPY ["src/LiaXP.Infrastructure/LiaXP.Infrastructure.csproj", "src/LiaXP.Infrastructure/"]
COPY ["src/LiaXP.Api/LiaXP.Api.csproj", "src/LiaXP.Api/"]
COPY ["tests/LiaXP.Tests/LiaXP.Tests.csproj", "tests/LiaXP.Tests/"]

# Restore dependencies
RUN dotnet restore "LiaXP.sln"

# Copy everything else and build
COPY . .
WORKDIR "/src/src/LiaXP.Api"
RUN dotnet build "LiaXP.Api.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "LiaXP.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
EXPOSE 80
EXPOSE 443

# Install timezone data
RUN apt-get update && apt-get install -y tzdata && rm -rf /var/lib/apt/lists/*

COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "LiaXP.Api.dll"]
