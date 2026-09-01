FROM mcr.microsoft.com/dotnet/runtime:10.0 AS base
USER $APP_UID
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["App_Tests/App_Tests.csproj", "App_Tests/"]
COPY ["App_BLL/App_BLL.csproj", "App_BLL/"]
COPY ["App_DAL/App_DAL.csproj", "App_DAL/"]
COPY ["App_Common/App_Common.csproj", "App_Common/"]
RUN dotnet restore "App_Tests/App_Tests.csproj"
COPY . .
WORKDIR "/src/App_Tests"
RUN dotnet build "./App_Tests.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./App_Tests.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "App_Tests.dll"]
