using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoffeeBar.Migrations
{
    /// <inheritdoc />
    public partial class AddGelatoAndPayInfoPrices : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "CurrentGelatoPrice",
                table: "PayInfos",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "CurrentHighGelatoPrice",
                table: "PayInfos",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.CreateTable(
                name: "Gelatos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ObjID = table.Column<string>(type: "TEXT", nullable: true),
                    MemberObjID = table.Column<string>(type: "TEXT", nullable: true),
                    MemberId = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Gelatos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Gelatos_Members_MemberId",
                        column: x => x.MemberId,
                        principalTable: "Members",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Gelatos_MemberId",
                table: "Gelatos",
                column: "MemberId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Gelatos");

            migrationBuilder.DropColumn(
                name: "CurrentGelatoPrice",
                table: "PayInfos");

            migrationBuilder.DropColumn(
                name: "CurrentHighGelatoPrice",
                table: "PayInfos");
        }
    }
}
