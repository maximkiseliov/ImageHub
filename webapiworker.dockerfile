FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
USER app
WORKDIR /app
EXPOSE 5002

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

COPY ["Directory.Build.props", "./"]
COPY ["src/ImageHub.Application/ImageHub.Application.csproj", "ImageHub.Application/"]
COPY ["src/ImageHub.Domain/ImageHub.Domain.csproj", "ImageHub.Domain/"]
COPY ["src/ImageHub.Infrastructure/ImageHub.Infrastructure.csproj", "ImageHub.Infrastructure/"]
COPY ["src/ImageHub.Web.Api.Worker/ImageHub.Web.Api.Worker.csproj", "ImageHub.Web.Api.Worker/"]
RUN dotnet restore "ImageHub.Web.Api.Worker/ImageHub.Web.Api.Worker.csproj"

COPY src/. .

WORKDIR "/src/ImageHub.Web.Api.Worker"
RUN dotnet build "ImageHub.Web.Api.Worker.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "ImageHub.Web.Api.Worker.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "ImageHub.Web.Api.Worker.dll"]