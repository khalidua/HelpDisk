# 1. BUILD STAGE

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /src

COPY ["src/HelpDisk.API/HelpDisk.API.csproj", "src/HelpDisk.API/"]
COPY ["src/HelpDisk.Application/HelpDisk.Application.csproj", "src/HelpDisk.Application/"]
COPY ["src/HelpDisk.Domain/HelpDisk.Domain.csproj", "src/HelpDisk.Domain/"]
COPY ["src/HelpDisk.Infrastructure/HelpDisk.Infrastructure.csproj", "src/HelpDisk.Infrastructure/"]
RUN dotnet restore "src/HelpDisk.API/HelpDisk.API.csproj"
COPY . .
RUN dotnet publish "src/HelpDisk.API/HelpDisk.API.csproj" \
    -c Release \
    -o /app/publish \
    --no-restore


# 2. RUNTIME STAGE

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final

WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "HelpDisk.API.dll"]