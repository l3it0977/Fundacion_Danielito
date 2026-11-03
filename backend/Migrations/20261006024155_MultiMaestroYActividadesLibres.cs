using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SISTEMADANIELITO.Migrations
{
    /// <inheritdoc />
    public partial class MultiMaestroYActividadesLibres : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // El orden importa: primero se crea la tabla nueva y se copian los maestros
            // existentes, y recién después se borra la columna de la que salieron.
            // Al revés, cada grupo perdería a su maestro actual.

            migrationBuilder.DropTable(
                name: "Actividad_Grupos");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Asistencias_Estado",
                table: "Asistencias");

            migrationBuilder.CreateTable(
                name: "Grupo_Maestros",
                columns: table => new
                {
                    GrupoId = table.Column<int>(type: "integer", nullable: false),
                    PerfilId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Grupo_Maestros", x => new { x.GrupoId, x.PerfilId });
                    table.ForeignKey(
                        name: "FK_Grupo_Maestros_Grupos_GrupoId",
                        column: x => x.GrupoId,
                        principalTable: "Grupos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Grupo_Maestros_Perfiles_PerfilId",
                        column: x => x.PerfilId,
                        principalTable: "Perfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            // Cada grupo conserva el maestro que ya tenía, ahora como primera fila
            // de su lista de maestros.
            migrationBuilder.Sql(@"
                INSERT INTO ""Grupo_Maestros"" (""GrupoId"", ""PerfilId"")
                SELECT ""Id"", ""MaestroId"" FROM ""Grupos"";");

            migrationBuilder.DropForeignKey(
                name: "FK_Grupos_Perfiles_MaestroId",
                table: "Grupos");

            migrationBuilder.DropIndex(
                name: "IX_Grupos_MaestroId",
                table: "Grupos");

            migrationBuilder.DropColumn(
                name: "MaestroId",
                table: "Grupos");

            // La fecha pasa a ser obligatoria. A las actividades que no tenían se les
            // pone la fecha de hoy: dejarlas en el año 1, que es lo que haría el valor
            // por defecto, las sacaría de cualquier reporte por mes.
            migrationBuilder.Sql(@"
                UPDATE ""Actividades""
                SET ""Fecha_Actividad"" = CURRENT_DATE
                WHERE ""Fecha_Actividad"" IS NULL;");

            migrationBuilder.AlterColumn<DateTime>(
                name: "Fecha_Actividad",
                table: "Actividades",
                type: "timestamp without time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified),
                oldClrType: typeof(DateTime),
                oldType: "timestamp without time zone",
                oldNullable: true);

            // Se agrega 'pendiente': el estado con el que nace la lista de asistencia.
            migrationBuilder.AddCheckConstraint(
                name: "CK_Asistencias_Estado",
                table: "Asistencias",
                sql: "\"Estado\" IN ('pendiente', 'presente', 'ausente', 'justificado')");

            migrationBuilder.CreateIndex(
                name: "IX_Grupo_Maestros_PerfilId",
                table: "Grupo_Maestros",
                column: "PerfilId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Grupo_Maestros");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Asistencias_Estado",
                table: "Asistencias");

            migrationBuilder.AddColumn<int>(
                name: "MaestroId",
                table: "Grupos",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<DateTime>(
                name: "Fecha_Actividad",
                table: "Actividades",
                type: "timestamp without time zone",
                nullable: true,
                oldClrType: typeof(DateTime),
                oldType: "timestamp without time zone");

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

            migrationBuilder.CreateIndex(
                name: "IX_Grupos_MaestroId",
                table: "Grupos",
                column: "MaestroId");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Asistencias_Estado",
                table: "Asistencias",
                sql: "\"Estado\" IN ('presente', 'ausente', 'justificado')");

            migrationBuilder.CreateIndex(
                name: "IX_Actividad_Grupos_GrupoId",
                table: "Actividad_Grupos",
                column: "GrupoId");

            migrationBuilder.AddForeignKey(
                name: "FK_Grupos_Perfiles_MaestroId",
                table: "Grupos",
                column: "MaestroId",
                principalTable: "Perfiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
