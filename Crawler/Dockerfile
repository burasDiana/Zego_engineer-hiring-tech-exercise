FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build

WORKDIR /app

COPY Crawler.sln ./
COPY src/Crawler.Core/Crawler.Core.csproj src/Crawler.Core/
COPY src/Crawler/Crawler.csproj src/Crawler/
COPY tests/Crawler.Tests/Crawler.Tests.csproj tests/Crawler.Tests/

RUN dotnet restore

COPY . .

RUN dotnet build -c Release --no-restore

RUN dotnet test tests/Crawler.Tests/Crawler.Tests.csproj \
    --configuration Release \
    --no-build \
    --logger "trx;LogFileName=test_results.trx"

FROM build AS publish
RUN dotnet publish src/Crawler/Crawler.csproj \
    -c Release \
    -o /app/publish \
    --no-build

FROM mcr.microsoft.com/dotnet/runtime:10.0 AS final

WORKDIR /app
COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "Crawler.dll"]
CMD ["https://web-scraping.dev", "--concurrency", "8"]
