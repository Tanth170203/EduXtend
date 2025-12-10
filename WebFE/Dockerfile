# Use .NET SDK for building
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base 
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build 
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy project files
COPY ["WebFE/WebFE.csproj", "WebFE/"]
COPY ["BusinessObject/BusinessObject.csproj", "BusinessObject/"]
COPY ["Services/Services.csproj", "Services/"]
COPY ["Repositories/Repositories.csproj", "Repositories/"]
COPY ["DataAccess/DataAccess.csproj", "DataAccess/"]
COPY ["Utils/Utils.csproj", "Utils/"]

# Restore dependencies
RUN dotnet restore "WebFE/WebFE.csproj"

# Copy all source code
COPY . .
WORKDIR "/src/WebFE"

# Build the application
RUN dotnet build "WebFE.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Publish the application
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "WebFE.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

# Final stage - runtime
FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
# Start the application
ENV ASPNETCORE_ENVIRONMENT=Development
ENTRYPOINT ["dotnet", "WebFE.dll"]
