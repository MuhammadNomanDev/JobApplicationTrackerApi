FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
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
