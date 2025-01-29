using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.AspNetCore.Identity;

#nullable disable

namespace ManageEmployees.Api.Migrations
{
    public partial class SeedDirectorUser : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Users", 
                columns: new[]
                {
                    "Id",
                    "FirstName",
                    "LastName",
                    "Email",
                    "NormalizedEmail",
                    "UserName",
                    "NormalizedUserName",
                    "EmailConfirmed",
                    "PasswordHash",
                    "SecurityStamp",
                    "PhoneNumberConfirmed", 
                    "TwoFactorEnabled", 
                    "LockoutEnabled", 
                    "AccessFailedCount"
                },
                values: new object[]
                {
                    "1e02c0f5-4c65-4b0e-9a76-5550a973847d",
                    "Default",
                    "Director",
                    "admin@company.com",
                    "ADMIN@COMPANY.COM",
                    "admin@company.com",
                    "ADMIN@COMPANY.COM",
                    true,
                    HashPassword("Admin123!"),
                    Guid.NewGuid().ToString(),
                    false, 
                    false, 
                    false, 
                    0
                });

            migrationBuilder.InsertData(
                table: "UserRoles", 
                columns: new[] { "UserId", "RoleId" },
                values: new object[]
                {
                    "1e02c0f5-4c65-4b0e-9a76-5550a973847d", 
                    "ad9c4843-fe68-4854-ab4c-8bd22ac2160b"  
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "UserRoles",
                keyColumns: new[] { "UserId", "RoleId" },
                keyValues: new object[]
                {
                    "1e02c0f5-4c65-4b0e-9a76-5550a973847d",
                    "ad9c4843-fe68-4854-ab4c-8bd22ac2160b" 
                });

            migrationBuilder.DeleteData(
                table: "Users",
                keyColumn: "Id",
                keyValue: "1e02c0f5-4c65-4b0e-9a76-5550a973847d");
        }

        private string HashPassword(string password)
        {
            var hasher = new PasswordHasher<object>();
            return hasher.HashPassword(null, password);
        }
    }
}
