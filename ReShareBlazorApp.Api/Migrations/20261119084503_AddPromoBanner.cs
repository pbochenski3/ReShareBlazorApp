using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ReShareBlazorApp.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddPromoBanner : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PromoBanners",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Message = table.Column<string>(type: "nvarchar(250)", maxLength: 250, nullable: false),
                    BadgeText = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    LinkUrl = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: true),
                    LinkText = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PromoBanners", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PromoBanners");
        }
    }
}
