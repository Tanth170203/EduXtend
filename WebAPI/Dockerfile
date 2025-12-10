# Use .NET SDK for building
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base 
WORKDIR /app
EXPOSE 80

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build 
ARG BUILD_CONFIGURATION=Release
WORKDIR /src

# Copy project files
COPY ["WebAPI/WebAPI.csproj", "WebAPI/"]
COPY ["Services/Services.csproj", "Services/"]
COPY ["BusinessObject/BusinessObject.csproj", "BusinessObject/"]
COPY ["Repositories/Repositories.csproj", "Repositories/"]
COPY ["DataAccess/DataAccess.csproj", "DataAccess/"]
COPY ["Utils/Utils.csproj", "Utils/"]

# Restore dependencies
RUN dotnet restore "WebAPI/WebAPI.csproj"
COPY . .
WORKDIR "/src/WebAPI"
RUN dotnet build "WebAPI.csproj" -c $BUILD_CONFIGURATION -o /app/build

# Use the .NET Runtime image for running the app
FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "WebAPI.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
# Start the application
ENV ASPNETCORE_ENVIRONMENT=Development
ENTRYPOINT ["dotnet", "WebAPI.dll"]
