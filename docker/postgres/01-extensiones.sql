-- =============================================================================
-- Inicialización de la base de datos de PlanVibe.
-- Este script solo se ejecuta la PRIMERA vez que se crea el volumen de datos.
-- Si se modifica, hay que borrar el volumen para que vuelva a aplicarse:
--     docker compose down -v
-- =============================================================================

-- PostGIS: tipos geográficos e índices espaciales. Es lo que permite resolver
-- «qué planes hay a menos de 5 km» por índice en lugar de recorriendo la tabla.
CREATE EXTENSION IF NOT EXISTS postgis;

-- Búsqueda de texto tolerante a erratas y a acentos, para el buscador de planes.
CREATE EXTENSION IF NOT EXISTS pg_trgm;
CREATE EXTENSION IF NOT EXISTS unaccent;

-- -----------------------------------------------------------------------------
-- Esquemas. Separan datos públicos, datos de cuenta y traza de auditoría, según
-- el principio recogido en docs/02-arquitectura.md.
--
-- Las migraciones de EF Core crean las tablas dentro de estos esquemas.
-- -----------------------------------------------------------------------------
CREATE SCHEMA IF NOT EXISTS app;
CREATE SCHEMA IF NOT EXISTS identidad;
CREATE SCHEMA IF NOT EXISTS auditoria;

-- -----------------------------------------------------------------------------
-- PENDIENTE PARA PRODUCCIÓN: separación de privilegios.
--
-- En este entorno local, la aplicación se conecta con el usuario propietario de
-- la base de datos, que puede modificar el esquema. Es cómodo para desarrollar,
-- porque las migraciones se aplican solas al arrancar, pero NO debe replicarse
-- en producción: si una inyección de SQL llegara a ejecutarse, tendría control
-- total del motor.
--
-- En producción hay que crear dos usuarios distintos:
--
--   * planvibe_migraciones — propietario del esquema. Lo usa únicamente el paso
--     de despliegue que aplica las migraciones, nunca la aplicación en marcha.
--
--   * planvibe_app — el que usa la API. Con SELECT, INSERT, UPDATE y DELETE
--     sobre app e identidad, y solo SELECT e INSERT sobre auditoria: un registro
--     de auditoría que la propia aplicación puede reescribir no sirve como registro.
--
-- Ver docs/09-modelo-de-amenazas.md.
-- -----------------------------------------------------------------------------
