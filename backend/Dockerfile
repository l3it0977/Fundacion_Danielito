# API del Sistema Danielito. Se construye con el contexto en esta carpeta (backend/).

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS compilar
WORKDIR /src

# El restore va primero y solo con el .csproj: mientras no cambien los paquetes, Docker reutiliza
# esta capa y no los vuelve a descargar en cada compilación.
COPY SISTEMADANIELITO.csproj ./
RUN dotnet restore

COPY . .
RUN dotnet publish -c Release -o /app/publicar --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS final
WORKDIR /app

# QuestPDF dibuja los PDF con Skia, que en Linux necesita fontconfig.
RUN apt-get update \
    && apt-get install -y --no-install-recommends libfontconfig1 \
    && rm -rf /var/lib/apt/lists/*

ENV ASPNETCORE_ENVIRONMENT=Production \
    ASPNETCORE_HTTP_PORTS=8080

COPY --from=compilar /app/publicar .

# La imagen trae un usuario sin privilegios: la API no necesita ser root.
USER $APP_UID

EXPOSE 8080
ENTRYPOINT ["dotnet", "SISTEMADANIELITO.dll"]
