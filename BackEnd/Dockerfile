FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copia os ficheiros de projeto (.csproj) individualmente para otimizar o cache do Docker
COPY ["MarketplaceArtesanato.API/MarketplaceArtesanato.API.csproj", "MarketplaceArtesanato.API/"]
COPY ["MarketplaceArtesanato.Core/MarketplaceArtesanato.Core.csproj", "MarketplaceArtesanato.Core/"]
COPY ["MarketplaceArtesanato.Data/MarketplaceArtesanato.Data.csproj", "MarketplaceArtesanato.Data/"]
COPY ["MarketplaceArtesanato.Services/MarketplaceArtesanato.Services.csproj", "MarketplaceArtesanato.Services/"]
COPY ["MarketplaceArtesanato.Infrastructure/MarketplaceArtesanato.Infrastructure.csproj", "MarketplaceArtesanato.Infrastructure/"]

RUN dotnet restore "MarketplaceArtesanato.API/MarketplaceArtesanato.API.csproj"

COPY . .

WORKDIR "/src/MarketplaceArtesanato.API"
RUN dotnet build "MarketplaceArtesanato.API.csproj" -c Release -o /app/build

# Publica a aplicação (gera os ficheiros prontos para rodar)
FROM build AS publish
RUN dotnet publish "MarketplaceArtesanato.API.csproj" -c Release -o /app/publish /p:UseAppHost=false


FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "MarketplaceArtesanato.API.dll"]