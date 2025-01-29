using ManageEmployees.Domain;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ManageEmployees.Api.Migrations
{
    /// <inheritdoc />
    public partial class SeedRoles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(table: TableName.Roles,
                columns: new[] { TableColumn.Id, TableColumn.Name, TableColumn.NormalizedName },
                values: new object[,]
                {
                { "ad9c4843-fe68-4854-ab4c-8bd22ac2160b", RoleName.Director, RoleName.Director.ToUpper() },
                { "780cc6f7-e426-4bcd-9c72-86ea4b4537d4", RoleName.Leader, RoleName.Leader.ToUpper() },
                { "42227e59-1810-4fe6-baf1-c977a1c20252", RoleName.Employee, RoleName.Employee.ToUpper() },
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                            table: TableName.Roles,
                            keyColumn: TableColumn.Name,
                            keyValues: new object[] { 
                            RoleName.Director,
                            RoleName.Leader,
                            RoleName.Employee}.Cast<object>().ToArray());
        }
    }
}
