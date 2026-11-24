using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Infrastructure;
using ReShareBlazorApp.Api.Date;

#nullable disable

namespace ReShareBlazorApp.Api.Migrations
{
    [DbContext(typeof(ReshareDbContext))]
    [Migration("20261124090000_AddNotificationActorName")]
    public partial class AddNotificationActorName : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ActorName",
                table: "Notifications",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ActorName",
                table: "Notifications");
        }
    }
}
