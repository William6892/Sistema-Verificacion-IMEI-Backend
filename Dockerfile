FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY . .

# Ahora solo hay un .csproj, pero igual debes especificarlo
RUN dotnet restore "Sistema de Verificación IMEI.csproj"
RUN dotnet publish "Sistema de Verificación IMEI.csproj" -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/aspnet:9.0
WORKDIR /app
COPY --from=build /app/publish .

EXPOSE 80
ENTRYPOINT ["dotnet", "Sistema de Verificación IMEI.dll"]