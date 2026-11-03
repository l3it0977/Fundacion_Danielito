using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SISTEMADANIELITO.Migrations
{
    /// <inheritdoc />
    public partial class IndicesPorFecha : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Asistencias_Fecha",
                table: "Asistencias",
                column: "Fecha");

            migrationBuilder.CreateIndex(
                name: "IX_Actividades_Fecha_Actividad",
                table: "Actividades",
                column: "Fecha_Actividad");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Asistencias_Fecha",
                table: "Asistencias");

            migrationBuilder.DropIndex(
                name: "IX_Actividades_Fecha_Actividad",
                table: "Actividades");
        }
    }
}
