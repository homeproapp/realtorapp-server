FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY RealtorApp.slnx .
COPY src/RealtorApp.Api/RealtorApp.Api.csproj src/RealtorApp.Api/
COPY src/RealtorApp.Contracts/RealtorApp.Contracts.csproj src/RealtorApp.Contracts/
COPY src/RealtorApp.Domain/RealtorApp.Domain.csproj src/RealtorApp.Domain/
COPY src/RealtorApp.Infra/RealtorApp.Infra.csproj src/RealtorApp.Infra/

RUN dotnet restore src/RealtorApp.Api/RealtorApp.Api.csproj
RUN dotnet restore src/RealtorApp.Domain/RealtorApp.Domain.csproj

COPY src/ src/

WORKDIR /src/src/RealtorApp.Api
RUN dotnet publish -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app

RUN groupadd -r appuser && useradd -r -g appuser appuser

COPY --from=build /app/publish .

RUN chown -R appuser:appuser /app

USER appuser

EXPOSE 8080
EXPOSE 8081

ENV ASPNETCORE_URLS=http://+:8080;https://+:8081
ENV ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "RealtorApp.Api.dll"]
