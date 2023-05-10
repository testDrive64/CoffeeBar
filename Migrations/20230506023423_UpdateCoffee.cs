using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoffeeBar.Migrations
{
    /// <inheritdoc />
    public partial class UpdateCoffee : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CoffeeID",
                table: "Coffees");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CoffeeID",
                table: "Coffees",
                type: "TEXT",
                nullable: true);
        }
    }
}
