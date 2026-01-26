using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BasicFinance.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AdjustDataSpreadsheet : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "GoogleSheetId",
                table: "DataSpreadsheets",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "GoogleSheetId",
                table: "DataSpreadsheets");
        }
    }
}
