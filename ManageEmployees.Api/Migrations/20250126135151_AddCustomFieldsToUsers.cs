using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ManageEmployees.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddCustomFieldsToUsers : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Adicionar a coluna FirstName
            migrationBuilder.AddColumn<string>(
                name: "FirstName",
                table: "Users",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: false,
                defaultValue: "");

            // Adicionar a coluna LastName
            migrationBuilder.AddColumn<string>(
                name: "LastName",
                table: "Users",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: false,
                defaultValue: "");

            // Adicionar a coluna DocNumber
            migrationBuilder.AddColumn<string>(
                name: "DocNumber",
                table: "Users",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: false,
                defaultValue: "");

            // Criar índice único para DocNumber
            migrationBuilder.CreateIndex(
                name: "IX_Users_DocNumber",
                table: "Users",
                column: "DocNumber",
                unique: true);

            // Adicionar a coluna ManagerId
            migrationBuilder.AddColumn<string>(
                name: "ManagerId",
                table: "Users",
                type: "nvarchar(450)",
                nullable: true);

            // Configurar chave estrangeira para ManagerId
            migrationBuilder.AddForeignKey(
                name: "FK_Users_Manager",
                table: "Users",
                column: "ManagerId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Remover chave estrangeira para ManagerId
            migrationBuilder.DropForeignKey(
                name: "FK_Users_Manager",
                table: "Users");

            // Remover índice para DocNumber
            migrationBuilder.DropIndex(
                name: "IX_Users_DocNumber",
                table: "Users");

            // Remover a coluna FirstName
            migrationBuilder.DropColumn(
                name: "FirstName",
                table: "Users");

            // Remover a coluna LastName
            migrationBuilder.DropColumn(
                name: "LastName",
                table: "Users");

            // Remover a coluna DocNumber
            migrationBuilder.DropColumn(
                name: "DocNumber",
                table: "Users");

            // Remover a coluna ManagerId
            migrationBuilder.DropColumn(
                name: "ManagerId",
                table: "Users");
        }
    }
}
