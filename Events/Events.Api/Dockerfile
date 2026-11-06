# multi-stage: сборка Events.Api и лёгкий runtime-образ
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Shared/Shared.Contracts/Shared.Contracts.csproj Shared/Shared.Contracts/
COPY Events/Events.Domain/Events.Domain.csproj Events/Events.Domain/
COPY Events/Events.Application/Events.Application.csproj Events/Events.Application/
COPY Events/Events.Infrastructure/Events.Infrastructure.csproj Events/Events.Infrastructure/
COPY Events/Events.Api/Events.Api.csproj Events/Events.Api/

RUN dotnet restore Events/Events.Api/Events.Api.csproj

COPY Shared/ Shared/
COPY Events/ Events/

RUN dotnet publish Events/Events.Api/Events.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Events.Api.dll"]
