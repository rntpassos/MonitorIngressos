# Etapa 1: Compilação
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY ["MonitorIngressos.csproj", "./"]
RUN dotnet restore "MonitorIngressos.csproj"

COPY . .
RUN dotnet publish "MonitorIngressos.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Etapa 2: Imagem final de runtime enxuta
FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS final
WORKDIR /app

RUN apk add --no-cache tzdata
ENV TZ=America/Sao_Paulo

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "MonitorIngressos.dll"]
