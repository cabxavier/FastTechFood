FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

WORKDIR /app

COPY ./ ./

RUN dotnet restore

RUN dotnet build --configuration Release --no-restore

RUN dotnet test --logger "trx;LogFileName=test-results.trx" --results-directory /app/test-results

RUN dotnet publish -c Release -o out

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS root

COPY --from=build /app/out .
EXPOSE 5000

ENTRYPOINT ["dotnet", "FastTechFood.API.dll"]