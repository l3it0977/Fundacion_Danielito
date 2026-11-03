using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SISTEMADANIELITO.Migrations
{
    /// <inheritdoc />
    public partial class AgregarEntidadesDominio : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Actividades",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nombre = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Descripcion = table.Column<string>(type: "text", nullable: true),
                    Fecha_Actividad = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    Tipo = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Activa = table.Column<bool>(type: "boolean", nullable: false),
                    CreadoPorId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Actividades", x => x.Id);
                    table.CheckConstraint("CK_Actividades_Tipo", "\"Tipo\" IN ('normal', 'intervencion')");
                    table.ForeignKey(
                        name: "FK_Actividades_Perfiles_CreadoPorId",
                        column: x => x.CreadoPorId,
                        principalTable: "Perfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Grupos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nombre = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Descripcion = table.Column<string>(type: "text", nullable: true),
                    Activo = table.Column<bool>(type: "boolean", nullable: false),
                    MaestroId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Grupos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Grupos_Perfiles_MaestroId",
                        column: x => x.MaestroId,
                        principalTable: "Perfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Actividad_Grupos",
                columns: table => new
                {
                    ActividadId = table.Column<int>(type: "integer", nullable: false),
                    GrupoId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Actividad_Grupos", x => new { x.ActividadId, x.GrupoId });
                    table.ForeignKey(
                        name: "FK_Actividad_Grupos_Actividades_ActividadId",
                        column: x => x.ActividadId,
                        principalTable: "Actividades",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Actividad_Grupos_Grupos_GrupoId",
                        column: x => x.GrupoId,
                        principalTable: "Grupos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Ninos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Codigo = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Nombres = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Apellidos = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Fecha_Nacimiento = table.Column<DateTime>(type: "timestamp without time zone", nullable: true),
                    Contacto = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    Curso = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    Colegio = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    Vulnerable = table.Column<bool>(type: "boolean", nullable: false),
                    Motivo_Vulnerabilidad = table.Column<string>(type: "text", nullable: true),
                    Observaciones = table.Column<string>(type: "text", nullable: true),
                    Fue_Al_Encuentro = table.Column<bool>(type: "boolean", nullable: false),
                    Bautizado = table.Column<bool>(type: "boolean", nullable: false),
                    Asiste_Iglesia = table.Column<bool>(type: "boolean", nullable: false),
                    Nombre_Iglesia = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    Nombre_Celula = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    Activo = table.Column<bool>(type: "boolean", nullable: false),
                    Latitud = table.Column<decimal>(type: "numeric(10,8)", precision: 10, scale: 8, nullable: true),
                    Longitud = table.Column<decimal>(type: "numeric(11,8)", precision: 11, scale: 8, nullable: true),
                    GrupoId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Ninos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Ninos_Grupos_GrupoId",
                        column: x => x.GrupoId,
                        principalTable: "Grupos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Actividad_Ninos",
                columns: table => new
                {
                    ActividadId = table.Column<int>(type: "integer", nullable: false),
                    NinoId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Actividad_Ninos", x => new { x.ActividadId, x.NinoId });
                    table.ForeignKey(
                        name: "FK_Actividad_Ninos_Actividades_ActividadId",
                        column: x => x.ActividadId,
                        principalTable: "Actividades",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Actividad_Ninos_Ninos_NinoId",
                        column: x => x.NinoId,
                        principalTable: "Ninos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Asistencias",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Fecha = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Estado = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Motivo_Justificacion = table.Column<string>(type: "text", nullable: true),
                    Observacion = table.Column<string>(type: "text", nullable: true),
                    ActividadId = table.Column<int>(type: "integer", nullable: false),
                    NinoId = table.Column<int>(type: "integer", nullable: false),
                    RegistradoPorId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Asistencias", x => x.Id);
                    table.CheckConstraint("CK_Asistencias_Estado", "\"Estado\" IN ('presente', 'ausente', 'justificado')");
                    table.ForeignKey(
                        name: "FK_Asistencias_Actividades_ActividadId",
                        column: x => x.ActividadId,
                        principalTable: "Actividades",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Asistencias_Ninos_NinoId",
                        column: x => x.NinoId,
                        principalTable: "Ninos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Asistencias_Perfiles_RegistradoPorId",
                        column: x => x.RegistradoPorId,
                        principalTable: "Perfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Visitas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Fecha_Visita = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    Motivo = table.Column<string>(type: "text", nullable: true),
                    Observacion = table.Column<string>(type: "text", nullable: true),
                    Seguimiento = table.Column<string>(type: "text", nullable: true),
                    NinoId = table.Column<int>(type: "integer", nullable: false),
                    RealizadoPorId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Visitas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Visitas_Ninos_NinoId",
                        column: x => x.NinoId,
                        principalTable: "Ninos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Visitas_Perfiles_RealizadoPorId",
                        column: x => x.RealizadoPorId,
                        principalTable: "Perfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Actividad_Grupos_GrupoId",
                table: "Actividad_Grupos",
                column: "GrupoId");

            migrationBuilder.CreateIndex(
                name: "IX_Actividad_Ninos_NinoId",
                table: "Actividad_Ninos",
                column: "NinoId");

            migrationBuilder.CreateIndex(
                name: "IX_Actividades_CreadoPorId",
                table: "Actividades",
                column: "CreadoPorId");

            migrationBuilder.CreateIndex(
                name: "IX_Asistencias_ActividadId_NinoId_Fecha",
                table: "Asistencias",
                columns: new[] { "ActividadId", "NinoId", "Fecha" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Asistencias_NinoId",
                table: "Asistencias",
                column: "NinoId");

            migrationBuilder.CreateIndex(
                name: "IX_Asistencias_RegistradoPorId",
                table: "Asistencias",
                column: "RegistradoPorId");

            migrationBuilder.CreateIndex(
                name: "IX_Grupos_MaestroId",
                table: "Grupos",
                column: "MaestroId");

            migrationBuilder.CreateIndex(
                name: "IX_Ninos_Codigo",
                table: "Ninos",
                column: "Codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Ninos_GrupoId",
                table: "Ninos",
                column: "GrupoId");

            migrationBuilder.CreateIndex(
                name: "IX_Visitas_NinoId",
                table: "Visitas",
                column: "NinoId");

            migrationBuilder.CreateIndex(
                name: "IX_Visitas_RealizadoPorId",
                table: "Visitas",
                column: "RealizadoPorId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Actividad_Grupos");

            migrationBuilder.DropTable(
                name: "Actividad_Ninos");

            migrationBuilder.DropTable(
                name: "Asistencias");

            migrationBuilder.DropTable(
                name: "Visitas");

            migrationBuilder.DropTable(
                name: "Actividades");

            migrationBuilder.DropTable(
                name: "Ninos");

            migrationBuilder.DropTable(
                name: "Grupos");
        }
    }
}
