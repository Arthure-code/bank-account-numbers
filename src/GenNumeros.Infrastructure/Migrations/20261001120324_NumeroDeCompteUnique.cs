using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace GenNumeros.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class NumeroDeCompteUnique : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Statut",
                table: "NumeroDossiers",
                type: "TEXT",
                maxLength: 20,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "NumeroCompte",
                table: "NumeroDossiers",
                type: "TEXT",
                maxLength: 19,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "IdDemandeur",
                table: "NumeroDossiers",
                type: "TEXT",
                maxLength: 50,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_NumeroDossiers_NumeroCompte",
                table: "NumeroDossiers",
                column: "NumeroCompte",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_NumeroDossiers_NumeroCompte",
                table: "NumeroDossiers");

            migrationBuilder.AlterColumn<string>(
                name: "Statut",
                table: "NumeroDossiers",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldMaxLength: 20);

            migrationBuilder.AlterColumn<string>(
                name: "NumeroCompte",
                table: "NumeroDossiers",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldMaxLength: 19);

            migrationBuilder.AlterColumn<string>(
                name: "IdDemandeur",
                table: "NumeroDossiers",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldMaxLength: 50);
        }
    }
}
