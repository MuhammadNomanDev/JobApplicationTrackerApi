FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
# Central package management: the props files and SDK pin must exist before
# restore. Without them, TargetFramework is empty and package versions are
# unknown (NETSDK1013). Fixed 2026-10-06 after the M1a CPM move broke this.
COPY ["Directory.Packages.props", "./"]
COPY ["Directory.Build.props", "./"]
COPY ["global.json", "./"]
COPY ["src/JobApplicationTrackerAPI.Api/JobApplicationTrackerAPI.Api.csproj", "src/JobApplicationTrackerAPI.Api/"]
COPY ["src/JobApplicationTrackerAPI.Application/JobApplicationTrackerAPI.Application.csproj", "src/JobApplicationTrackerAPI.Application/"]
COPY ["src/JobApplicationTrackerAPI.Domain/JobApplicationTrackerAPI.Domain.csproj", "src/JobApplicationTrackerAPI.Domain/"]
COPY ["src/JobApplicationTrackerAPI.Infrastructure/JobApplicationTrackerAPI.Infrastructure.csproj", "src/JobApplicationTrackerAPI.Infrastructure/"]
RUN dotnet restore "src/JobApplicationTrackerAPI.Api/JobApplicationTrackerAPI.Api.csproj"
COPY . .
WORKDIR "/src/src/JobApplicationTrackerAPI.Api"
RUN dotnet build "JobApplicationTrackerAPI.Api.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "JobApplicationTrackerAPI.Api.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "JobApplicationTrackerAPI.Api.dll"]
