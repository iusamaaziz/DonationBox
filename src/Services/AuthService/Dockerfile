# syntax=docker/dockerfile:1

ARG BUILD_CONFIGURATION=Release

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy csproj and restore as distinct layers
COPY ["src/Services/AuthService/AuthService.csproj", "src/Services/AuthService/"]
RUN dotnet restore "src/Services/AuthService/AuthService.csproj"

# Copy everything else and publish
COPY . .
WORKDIR "/src/src/Services/AuthService"
RUN dotnet publish "AuthService.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
EXPOSE 8080 8443

# Configure ASP.NET Core for container
ENV ASPNETCORE_URLS=http://+:8080;https://+:8443
ENV ASPNETCORE_ENVIRONMENT=Production

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "AuthService.dll"]


