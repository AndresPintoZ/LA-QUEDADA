# =============================================================================
# API de PlanVibe. Compilación en varias etapas: la imagen final no contiene
# el SDK, ni el código fuente, ni las herramientas de compilación.
# =============================================================================

# --- Etapa 1: restauración de dependencias -----------------------------------
# Se copian primero solo los archivos de proyecto. Así, mientras no cambien las
# dependencias, Docker reutiliza la capa de restauración y una recompilación
# tras tocar código tarda segundos en lugar de minutos.
FROM mcr.microsoft.com/dotnet/sdk:10.0-noble AS restauracion
WORKDIR /origen

# El .editorconfig es necesario para COMPILAR, no solo para dar formato: contiene
# las supresiones justificadas de analizadores y, como los avisos son errores, sin
# él la compilación falla aquí aunque funcione en local.
COPY Directory.Build.props Directory.Packages.props NuGet.config .editorconfig ./
COPY src/PlanVibe.Domain/PlanVibe.Domain.csproj src/PlanVibe.Domain/
COPY src/PlanVibe.Application/PlanVibe.Application.csproj src/PlanVibe.Application/
COPY src/PlanVibe.Infrastructure/PlanVibe.Infrastructure.csproj src/PlanVibe.Infrastructure/
COPY src/PlanVibe.Api/PlanVibe.Api.csproj src/PlanVibe.Api/

RUN dotnet restore src/PlanVibe.Api/PlanVibe.Api.csproj

# --- Etapa 2: compilación y publicación --------------------------------------
FROM restauracion AS publicacion
COPY src/ src/

RUN dotnet publish src/PlanVibe.Api/PlanVibe.Api.csproj \
    --configuration Release \
    --no-restore \
    --output /aplicacion

# --- Etapa 3: imagen final ---------------------------------------------------
# Imagen "chiseled": solo el runtime de ASP.NET, sin intérprete de órdenes ni
# gestor de paquetes. Reduce muchísimo la superficie de ataque, porque aunque
# alguien logre ejecutar algo dentro del contenedor no encontrará herramientas
# con las que moverse.
FROM mcr.microsoft.com/dotnet/aspnet:10.0-noble-chiseled AS final
WORKDIR /aplicacion

# La imagen chiseled ya trae el usuario sin privilegios "app" (UID 64198).
# Se declara explícitamente para que quede constancia: el proceso NUNCA corre como root.
USER app

COPY --from=publicacion --chown=app:app /aplicacion .

# Kestrel escucha en un puerto por encima de 1024 porque un usuario sin
# privilegios no puede abrir puertos bajos.
ENV ASPNETCORE_HTTP_PORTS=8080 \
    ASPNETCORE_ENVIRONMENT=Production \
    DOTNET_gcServer=1

EXPOSE 8080

# El healthcheck lo define docker-compose.yml, donde puede coordinarse con las
# dependencias entre servicios.
ENTRYPOINT ["dotnet", "PlanVibe.Api.dll"]
