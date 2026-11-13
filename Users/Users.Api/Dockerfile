# multi-stage: сборка Users.Api и лёгкий runtime-образ
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Users/Users.Domain/Users.Domain.csproj Users/Users.Domain/
COPY Users/Users.Application/Users.Application.csproj Users/Users.Application/
COPY Users/Users.Infrastructure/Users.Infrastructure.csproj Users/Users.Infrastructure/
COPY Users/Users.Api/Users.Api.csproj Users/Users.Api/

RUN dotnet restore Users/Users.Api/Users.Api.csproj

COPY Users/ Users/

RUN dotnet publish Users/Users.Api/Users.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Users.Api.dll"]
