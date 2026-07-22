FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /app

# Копируем файл проекта (он в корне)
COPY *.csproj .
RUN dotnet restore

# Копируем весь код (он в корне)
COPY . .
RUN dotnet publish -c Release -o out

# ==================================================
# ✅ ИСПРАВЛЕНИЕ: Устанавливаем библиотеки
# ==================================================
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

# ⭐ УСТАНАВЛИВАЕМ НЕОБХОДИМЫЕ БИБЛИОТЕКИ
RUN apt-get update && apt-get install -y \
    libgssapi-krb5-2 \
    libkrb5-3 \
    krb5-user \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/out .

EXPOSE 80
EXPOSE 443

ENTRYPOINT ["dotnet", "KhostgoriAPI.dll"]