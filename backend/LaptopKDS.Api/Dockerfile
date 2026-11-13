# ==========================================
# 1. Aşama: Derleme (Build Stage)
# ==========================================
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

# Proje dosyasını kopyala ve bağımlılıkları yükle (Docker cache optimizasyonu)
COPY ["LaptopKDS.Api.csproj", "./"]
RUN dotnet restore "LaptopKDS.Api.csproj"

# Tüm kaynak kodları kopyala ve derle
COPY . .
RUN dotnet publish "LaptopKDS.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# ==========================================
# 2. Aşama: Çalışma Zamanı (Runtime Stage)
# ==========================================
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# Gerekli curl aracı ve SQLite dizinini hazırla
USER root
RUN apt-get update && apt-get install -y --no-install-recommends curl && rm -rf /var/lib/apt/lists/*
RUN mkdir -p /app/data && chmod 777 /app/data

# Yayınlanan binary dosyaları kopyala
COPY --from=build /app/publish .

# Ortam değişkenleri
ENV ASPNETCORE_URLS=http://+:5000
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ConnectionStrings__DefaultConnection="Data Source=/app/data/laptopkds.db"

EXPOSE 5000

# Sağlık kontrolü
HEALTHCHECK --interval=10s --timeout=5s --start-period=10s --retries=3 \
  CMD curl -f http://localhost:5000/health || exit 1

ENTRYPOINT ["dotnet", "LaptopKDS.Api.dll"]
