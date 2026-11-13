# multi-stage: сборка Bookings.Api и лёгкий runtime-образ
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY Shared/Shared.Contracts/Shared.Contracts.csproj Shared/Shared.Contracts/
COPY Bookings/Bookings.Domain/Bookings.Domain.csproj Bookings/Bookings.Domain/
COPY Bookings/Bookings.Application/Bookings.Application.csproj Bookings/Bookings.Application/
COPY Bookings/Bookings.Infrastructure/Bookings.Infrastructure.csproj Bookings/Bookings.Infrastructure/
COPY Bookings/Bookings.Api/Bookings.Api.csproj Bookings/Bookings.Api/

RUN dotnet restore Bookings/Bookings.Api/Bookings.Api.csproj

COPY Shared/ Shared/
COPY Bookings/ Bookings/

RUN dotnet publish Bookings/Bookings.Api/Bookings.Api.csproj -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "Bookings.Api.dll"]
