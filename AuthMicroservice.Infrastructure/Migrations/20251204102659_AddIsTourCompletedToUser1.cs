using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AuthMicroservice.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddIsTourCompletedToUser1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsTourCompleted",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsTourCompleted",
                table: "Users");
        }
    }
}
