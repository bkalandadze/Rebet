FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["Rebet.sln", "./"]
COPY ["src/Rebet.Domain/Rebet.Domain.csproj", "src/Rebet.Domain/"]
COPY ["src/Rebet.Application/Rebet.Application.csproj", "src/Rebet.Application/"]
COPY ["src/Rebet.Infrastructure/Rebet.Infrastructure.csproj", "src/Rebet.Infrastructure/"]
COPY ["src/Rebet.Worker/Rebet.Worker.csproj", "src/Rebet.Worker/"]

RUN dotnet restore "src/Rebet.Worker/Rebet.Worker.csproj"

COPY ["src/Rebet.Domain/", "src/Rebet.Domain/"]
COPY ["src/Rebet.Application/", "src/Rebet.Application/"]
COPY ["src/Rebet.Infrastructure/", "src/Rebet.Infrastructure/"]
COPY ["src/Rebet.Worker/", "src/Rebet.Worker/"]

RUN dotnet publish "src/Rebet.Worker/Rebet.Worker.csproj" -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
EXPOSE 8081
ENTRYPOINT ["dotnet", "Rebet.Worker.dll"]
