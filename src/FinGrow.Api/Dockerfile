FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY Directory.Build.props Directory.Packages.props ./
COPY src/FinGrow.Domain/*.csproj src/FinGrow.Domain/
COPY src/FinGrow.Application/*.csproj src/FinGrow.Application/
COPY src/FinGrow.Infrastructure/*.csproj src/FinGrow.Infrastructure/
COPY src/FinGrow.Api/*.csproj src/FinGrow.Api/
RUN dotnet restore src/FinGrow.Api/FinGrow.Api.csproj

COPY src/ src/
RUN dotnet publish src/FinGrow.Api/FinGrow.Api.csproj -c Release -o /app --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=build /app .

USER $APP_UID

EXPOSE 8080
ENTRYPOINT ["dotnet", "FinGrow.Api.dll"]
