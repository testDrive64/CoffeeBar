using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoffeeBar.Migrations
{
    /// <inheritdoc />
    public partial class ReadOnlyAmountCoffee : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AmountCoffee",
                table: "Members");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "AmountCoffee",
                table: "Members",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }
    }
}
