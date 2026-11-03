using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SISTEMADANIELITO.Migrations
{
    /// <inheritdoc />
    public partial class ProtegerHistorialDelNino : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Actividad_Ninos_Ninos_NinoId",
                table: "Actividad_Ninos");

            migrationBuilder.DropForeignKey(
                name: "FK_Asistencias_Ninos_NinoId",
                table: "Asistencias");

            migrationBuilder.DropForeignKey(
                name: "FK_Visitas_Ninos_NinoId",
                table: "Visitas");

            migrationBuilder.AddForeignKey(
                name: "FK_Actividad_Ninos_Ninos_NinoId",
                table: "Actividad_Ninos",
                column: "NinoId",
                principalTable: "Ninos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Asistencias_Ninos_NinoId",
                table: "Asistencias",
                column: "NinoId",
                principalTable: "Ninos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Visitas_Ninos_NinoId",
                table: "Visitas",
                column: "NinoId",
                principalTable: "Ninos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Actividad_Ninos_Ninos_NinoId",
                table: "Actividad_Ninos");

            migrationBuilder.DropForeignKey(
                name: "FK_Asistencias_Ninos_NinoId",
                table: "Asistencias");

            migrationBuilder.DropForeignKey(
                name: "FK_Visitas_Ninos_NinoId",
                table: "Visitas");

            migrationBuilder.AddForeignKey(
                name: "FK_Actividad_Ninos_Ninos_NinoId",
                table: "Actividad_Ninos",
                column: "NinoId",
                principalTable: "Ninos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Asistencias_Ninos_NinoId",
                table: "Asistencias",
                column: "NinoId",
                principalTable: "Ninos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Visitas_Ninos_NinoId",
                table: "Visitas",
                column: "NinoId",
                principalTable: "Ninos",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
