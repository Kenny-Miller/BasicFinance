using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BasicFinance.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Accounts",
                columns: table => new
                {
                    AccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    AccountName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Balance = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Notes = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    LastUpdatedDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Institution = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    FinancialAccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    SystemCreatedDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    SystemModifiedDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Accounts", x => x.AccountId);
                });

            migrationBuilder.CreateTable(
                name: "DataSpreadsheets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    GoogleSheetId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    GoogleSheetName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    UserId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    SystemCreatedDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    SystemModifiedDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DataSpreadsheets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Transactions",
                columns: table => new
                {
                    TransactionId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    AccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    Date = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Description = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Category = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    SystemCreatedDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    SystemModifiedDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Transactions", x => x.TransactionId);
                    table.ForeignKey(
                        name: "FK_Transactions_Accounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Accounts",
                        principalColumn: "AccountId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_AccountId",
                table: "Transactions",
                column: "AccountId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DataSpreadsheets");

            migrationBuilder.DropTable(
                name: "Transactions");

            migrationBuilder.DropTable(
                name: "Accounts");
        }
    }
}
