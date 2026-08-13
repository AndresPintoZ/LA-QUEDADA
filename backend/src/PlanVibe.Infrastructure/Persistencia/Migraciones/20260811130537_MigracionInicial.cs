using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore.Migrations;
using NetTopologySuite.Geometries;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace PlanVibe.Infrastructure.Persistencia.Migraciones
{
    /// <inheritdoc />
    public partial class MigracionInicial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "app");

            migrationBuilder.EnsureSchema(
                name: "identidad");

            migrationBuilder.EnsureSchema(
                name: "auditoria");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:postgis", ",,");

            migrationBuilder.CreateTable(
                name: "categorias",
                schema: "app",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    nombre = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    clave = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    color_hex = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: false),
                    orden = table.Column<int>(type: "integer", nullable: false),
                    activa = table.Column<bool>(type: "boolean", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_categorias", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "cuentas_de_acceso",
                schema: "identidad",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CreadaEn = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    UserName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    PasswordHash = table.Column<string>(type: "text", nullable: true),
                    SecurityStamp = table.Column<string>(type: "text", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "text", nullable: true),
                    PhoneNumber = table.Column<string>(type: "text", nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "boolean", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cuentas_de_acceso", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "entradas",
                schema: "auditoria",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    actor_id = table.Column<Guid>(type: "uuid", nullable: true),
                    accion = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    tipo_de_objeto = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    objeto_id = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    metadatos = table.Column<string>(type: "jsonb", nullable: true),
                    ocurrido_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_entradas", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "quedadas",
                schema: "app",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    organizador_id = table.Column<Guid>(type: "uuid", nullable: false),
                    titulo = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    descripcion = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    categoria_id = table.Column<Guid>(type: "uuid", nullable: false),
                    normas = table.Column<string[]>(type: "text[]", nullable: false),
                    estado = table.Column<int>(type: "integer", nullable: false),
                    motivo_de_cancelacion = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    creada_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    actualizada_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ultimo_orden_de_llegada = table.Column<long>(type: "bigint", nullable: false),
                    ubicacion = table.Column<Point>(type: "geography(Point,4326)", nullable: true, computedColumnSql: "ST_SetSRID(ST_MakePoint(longitud, latitud), 4326)::geography", stored: true),
                    capacidad = table.Column<int>(type: "integer", nullable: false),
                    duracion = table.Column<TimeSpan>(type: "interval", nullable: false),
                    inicio = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    direccion_exacta = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    es_lugar_publico = table.Column<bool>(type: "boolean", nullable: false),
                    latitud = table.Column<double>(type: "double precision", nullable: false),
                    longitud = table.Column<double>(type: "double precision", nullable: false),
                    lugar = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    referencia = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_quedadas", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "roles",
                schema: "identidad",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_roles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "tokens_de_renovacion",
                schema: "identidad",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    usuario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    hash_del_token = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    familia = table.Column<Guid>(type: "uuid", nullable: false),
                    creado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    expira_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    usado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    revocado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    dispositivo = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_tokens_de_renovacion", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "usuarios",
                schema: "app",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    correo = table.Column<string>(type: "character varying(254)", maxLength: 254, nullable: false),
                    nombre_visible = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    ciudad = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: true),
                    biografia = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    anio_de_nacimiento = table.Column<int>(type: "integer", nullable: false),
                    estado = table.Column<int>(type: "integer", nullable: false),
                    verificacion_estado = table.Column<int>(type: "integer", nullable: false),
                    verificacion_proveedor = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: true),
                    verificacion_referencia = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    verificacion_mayoria_de_edad = table.Column<bool>(type: "boolean", nullable: false),
                    verificacion_actualizada_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    verificacion_observacion = table.Column<string>(type: "character varying(300)", maxLength: 300, nullable: true),
                    version_normas_aceptada = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    normas_aceptadas_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    creado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    actualizado_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    intereses = table.Column<List<string>>(type: "text[]", nullable: false),
                    roles = table.Column<int[]>(type: "integer[]", nullable: false),
                    xmin = table.Column<uint>(type: "xid", rowVersion: true, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_usuarios", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "cuentas_accesos_externos",
                schema: "identidad",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "text", nullable: false),
                    ProviderKey = table.Column<string>(type: "text", nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "text", nullable: true),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cuentas_accesos_externos", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_cuentas_accesos_externos_cuentas_de_acceso_UserId",
                        column: x => x.UserId,
                        principalSchema: "identidad",
                        principalTable: "cuentas_de_acceso",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "cuentas_reclamaciones",
                schema: "identidad",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClaimType = table.Column<string>(type: "text", nullable: true),
                    ClaimValue = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cuentas_reclamaciones", x => x.Id);
                    table.ForeignKey(
                        name: "FK_cuentas_reclamaciones_cuentas_de_acceso_UserId",
                        column: x => x.UserId,
                        principalSchema: "identidad",
                        principalTable: "cuentas_de_acceso",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "cuentas_tokens",
                schema: "identidad",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    LoginProvider = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Value = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cuentas_tokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_cuentas_tokens_cuentas_de_acceso_UserId",
                        column: x => x.UserId,
                        principalSchema: "identidad",
                        principalTable: "cuentas_de_acceso",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "asistencias",
                schema: "app",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false),
                    usuario_id = table.Column<Guid>(type: "uuid", nullable: false),
                    estado = table.Column<int>(type: "integer", nullable: false),
                    orden_de_llegada = table.Column<long>(type: "bigint", nullable: false),
                    solicitada_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    actualizada_en = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    quedada_id = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_asistencias", x => x.id);
                    table.ForeignKey(
                        name: "FK_asistencias_quedadas_quedada_id",
                        column: x => x.quedada_id,
                        principalSchema: "app",
                        principalTable: "quedadas",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "cuentas_roles",
                schema: "identidad",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    RoleId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cuentas_roles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_cuentas_roles_cuentas_de_acceso_UserId",
                        column: x => x.UserId,
                        principalSchema: "identidad",
                        principalTable: "cuentas_de_acceso",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_cuentas_roles_roles_RoleId",
                        column: x => x.RoleId,
                        principalSchema: "identidad",
                        principalTable: "roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "roles_reclamaciones",
                schema: "identidad",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RoleId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClaimType = table.Column<string>(type: "text", nullable: true),
                    ClaimValue = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_roles_reclamaciones", x => x.Id);
                    table.ForeignKey(
                        name: "FK_roles_reclamaciones_roles_RoleId",
                        column: x => x.RoleId,
                        principalSchema: "identidad",
                        principalTable: "roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "ix_asistencias_quedada_usuario",
                schema: "app",
                table: "asistencias",
                columns: new[] { "quedada_id", "usuario_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_asistencias_usuario",
                schema: "app",
                table: "asistencias",
                column: "usuario_id");

            migrationBuilder.CreateIndex(
                name: "ix_categorias_clave",
                schema: "app",
                table: "categorias",
                column: "clave",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_cuentas_accesos_externos_UserId",
                schema: "identidad",
                table: "cuentas_accesos_externos",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                schema: "identidad",
                table: "cuentas_de_acceso",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                schema: "identidad",
                table: "cuentas_de_acceso",
                column: "NormalizedUserName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_cuentas_reclamaciones_UserId",
                schema: "identidad",
                table: "cuentas_reclamaciones",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_cuentas_roles_RoleId",
                schema: "identidad",
                table: "cuentas_roles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "ix_auditoria_actor_fecha",
                schema: "auditoria",
                table: "entradas",
                columns: new[] { "actor_id", "ocurrido_en" });

            migrationBuilder.CreateIndex(
                name: "ix_auditoria_objeto",
                schema: "auditoria",
                table: "entradas",
                columns: new[] { "tipo_de_objeto", "objeto_id" });

            migrationBuilder.CreateIndex(
                name: "ix_quedadas_categoria",
                schema: "app",
                table: "quedadas",
                column: "categoria_id");

            migrationBuilder.CreateIndex(
                name: "ix_quedadas_estado",
                schema: "app",
                table: "quedadas",
                column: "estado");

            migrationBuilder.CreateIndex(
                name: "ix_quedadas_organizador",
                schema: "app",
                table: "quedadas",
                column: "organizador_id");

            migrationBuilder.CreateIndex(
                name: "ix_quedadas_ubicacion",
                schema: "app",
                table: "quedadas",
                column: "ubicacion")
                .Annotation("Npgsql:IndexMethod", "gist");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                schema: "identidad",
                table: "roles",
                column: "NormalizedName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_roles_reclamaciones_RoleId",
                schema: "identidad",
                table: "roles_reclamaciones",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "ix_tokens_expiracion",
                schema: "identidad",
                table: "tokens_de_renovacion",
                column: "expira_en");

            migrationBuilder.CreateIndex(
                name: "ix_tokens_familia",
                schema: "identidad",
                table: "tokens_de_renovacion",
                column: "familia");

            migrationBuilder.CreateIndex(
                name: "ix_tokens_hash",
                schema: "identidad",
                table: "tokens_de_renovacion",
                column: "hash_del_token",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_usuarios_correo",
                schema: "app",
                table: "usuarios",
                column: "correo",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "asistencias",
                schema: "app");

            migrationBuilder.DropTable(
                name: "categorias",
                schema: "app");

            migrationBuilder.DropTable(
                name: "cuentas_accesos_externos",
                schema: "identidad");

            migrationBuilder.DropTable(
                name: "cuentas_reclamaciones",
                schema: "identidad");

            migrationBuilder.DropTable(
                name: "cuentas_roles",
                schema: "identidad");

            migrationBuilder.DropTable(
                name: "cuentas_tokens",
                schema: "identidad");

            migrationBuilder.DropTable(
                name: "entradas",
                schema: "auditoria");

            migrationBuilder.DropTable(
                name: "roles_reclamaciones",
                schema: "identidad");

            migrationBuilder.DropTable(
                name: "tokens_de_renovacion",
                schema: "identidad");

            migrationBuilder.DropTable(
                name: "usuarios",
                schema: "app");

            migrationBuilder.DropTable(
                name: "quedadas",
                schema: "app");

            migrationBuilder.DropTable(
                name: "cuentas_de_acceso",
                schema: "identidad");

            migrationBuilder.DropTable(
                name: "roles",
                schema: "identidad");
        }
    }
}
