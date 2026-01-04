# Etapa 1: Build (Usa o SDK do .NET 9 para compilar)
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copia os ficheiros de projeto (.csproj) individualmente para otimizar o cache do Docker
COPY ["MarketplaceArtesanato.API/MarketplaceArtesanato.API.csproj", "MarketplaceArtesanato.API/"]
COPY ["MarketplaceArtesanato.Core/MarketplaceArtesanato.Core.csproj", "MarketplaceArtesanato.Core/"]
COPY ["MarketplaceArtesanato.Data/MarketplaceArtesanato.Data.csproj", "MarketplaceArtesanato.Data/"]
COPY ["MarketplaceArtesanato.Services/MarketplaceArtesanato.Services.csproj", "MarketplaceArtesanato.Services/"]
COPY ["MarketplaceArtesanato.Infrastructure/MarketplaceArtesanato.Infrastructure.csproj", "MarketplaceArtesanato.Infrastructure/"]

# Restaura as dependências de todos os projetos
RUN dotnet restore "MarketplaceArtesanato.API/MarketplaceArtesanato.API.csproj"

# Copia o restante do código fonte
COPY . .

# Compila o projeto API
WORKDIR "/src/MarketplaceArtesanato.API"
RUN dotnet build "MarketplaceArtesanato.API.csproj" -c Release -o /app/build

# Publica a aplicação (gera os ficheiros prontos para rodar)
FROM build AS publish
RUN dotnet publish "MarketplaceArtesanato.API.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Etapa 2: Runtime (Imagem final leve para execução)
# IMPORTANTE: Usamos aspnet:9.0 para ser compatível com o build
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

# Copia apenas o necessário da etapa de publicação
COPY --from=publish /app/publish .

# Comando para iniciar a aplicação
ENTRYPOINT ["dotnet", "MarketplaceArtesanato.API.dll"]