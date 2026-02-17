FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["Rebet.sln", "./"]
COPY ["src/Rebet.Domain/Rebet.Domain.csproj", "src/Rebet.Domain/"]
COPY ["src/Rebet.Application/Rebet.Application.csproj", "src/Rebet.Application/"]
COPY ["src/Rebet.Infrastructure/Rebet.Infrastructure.csproj", "src/Rebet.Infrastructure/"]
COPY ["src/Rebet.API/Rebet.API.csproj", "src/Rebet.API/"]

RUN dotnet restore "src/Rebet.API/Rebet.API.csproj"

COPY ["src/Rebet.Domain/", "src/Rebet.Domain/"]
COPY ["src/Rebet.Application/", "src/Rebet.Application/"]
COPY ["src/Rebet.Infrastructure/", "src/Rebet.Infrastructure/"]
COPY ["src/Rebet.API/", "src/Rebet.API/"]

RUN dotnet publish "src/Rebet.API/Rebet.API.csproj" -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "Rebet.API.dll"]
