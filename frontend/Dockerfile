# =============================================================================
# Aplicación web de PlanVibe. Compilación en varias etapas: la imagen final no
# contiene el código fuente ni las dependencias de desarrollo.
# =============================================================================

ARG VERSION_DE_NODE=22-alpine

# --- Etapa 1: dependencias ---------------------------------------------------
# Solo el manifiesto y el bloqueo de versiones. Mientras no cambien, Docker
# reutiliza esta capa y una recompilación tras tocar código tarda segundos.
FROM node:${VERSION_DE_NODE} AS dependencias
WORKDIR /aplicacion

COPY package.json package-lock.json ./

# npm ci instala exactamente lo que dice package-lock.json, sin resolver rangos.
# Es lo que hace que la imagen sea reproducible y que nadie pueda colar una versión
# distinta de una dependencia entre dos compilaciones.
RUN npm ci

# --- Etapa 2: compilación ----------------------------------------------------
FROM node:${VERSION_DE_NODE} AS compilacion
WORKDIR /aplicacion

COPY --from=dependencias /aplicacion/node_modules ./node_modules
COPY . .

# Variables que Next necesita para compilar. Son valores de sustitución: los reales
# llegan en tiempo de ejecución desde docker-compose. Aquí NO puede haber secretos,
# porque lo que se usa al compilar puede acabar dentro del paquete del navegador.
ARG NEXT_PUBLIC_NOMBRE_APP=PlanVibe
ENV NEXT_PUBLIC_NOMBRE_APP=${NEXT_PUBLIC_NOMBRE_APP} \
    NEXT_TELEMETRY_DISABLED=1 \
    SESION_SECRETO=valor-de-compilacion-sin-ningun-valor-real-32-caracteres \
    API_URL_INTERNA=http://api:8080

RUN npm run build

# --- Etapa 3: imagen final ---------------------------------------------------
FROM node:${VERSION_DE_NODE} AS final
WORKDIR /aplicacion

ENV NODE_ENV=production \
    NEXT_TELEMETRY_DISABLED=1 \
    PORT=3000 \
    HOSTNAME=0.0.0.0

# Usuario sin privilegios. La imagen de Node trae "node" (UID 1000) ya creado.
USER node

# La salida "standalone" incluye solo las dependencias que el servidor usa de verdad,
# no el node_modules completo: la imagen baja de más de 1 GB a poco más de 100 MB.
COPY --from=compilacion --chown=node:node /aplicacion/.next/standalone ./
COPY --from=compilacion --chown=node:node /aplicacion/.next/static ./.next/static
COPY --from=compilacion --chown=node:node /aplicacion/public ./public

EXPOSE 3000

# El healthcheck lo define docker-compose.yml, donde puede coordinarse con las
# dependencias entre servicios.
CMD ["node", "server.js"]
