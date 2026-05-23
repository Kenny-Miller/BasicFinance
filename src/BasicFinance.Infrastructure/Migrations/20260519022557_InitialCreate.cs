using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

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
                name: "AccountTypes",
                columns: table => new
                {
                    AccountTypeId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AccountTypeCode = table.Column<string>(type: "character varying(25)", maxLength: 25, nullable: false),
                    AccountTypeName = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    SystemCreatedDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    SystemModifiedDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountTypes", x => x.AccountTypeId);
                });

            migrationBuilder.CreateTable(
                name: "TransactionCategories",
                columns: table => new
                {
                    TransactionCategoryId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TransactionCategoryCode = table.Column<string>(type: "character varying(25)", maxLength: 25, nullable: false),
                    TransactionCategoryName = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    SystemCreatedDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    SystemModifiedDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TransactionCategories", x => x.TransactionCategoryId);
                });

            migrationBuilder.CreateTable(
                name: "TransactionTypes",
                columns: table => new
                {
                    TransactionTypeId = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    TransactionTypeCode = table.Column<string>(type: "character varying(25)", maxLength: 25, nullable: false),
                    TransactionTypeName = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    SystemCreatedDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    SystemModifiedDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TransactionTypes", x => x.TransactionTypeId);
                });

            migrationBuilder.CreateTable(
                name: "UserGoogleSpreadsheets",
                columns: table => new
                {
                    UserGoogleSpreadsheetId = table.Column<Guid>(type: "uuid", nullable: false),
                    GoogleSheetId = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    GoogleSheetName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    LastSyncedDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    UserId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    SystemCreatedDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    SystemModifiedDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserGoogleSpreadsheets", x => x.UserGoogleSpreadsheetId);
                });

            migrationBuilder.CreateTable(
                name: "Accounts",
                columns: table => new
                {
                    AccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserGoogleSpreadsheetId = table.Column<Guid>(type: "uuid", nullable: false),
                    AccountTypeId = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    AccountName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Balance = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Notes = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    BalanceRecordedDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Institution = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    FinancialAccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    SystemCreatedDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    SystemModifiedDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Accounts", x => x.AccountId);
                    table.ForeignKey(
                        name: "FK_Accounts_AccountTypes_AccountTypeId",
                        column: x => x.AccountTypeId,
                        principalTable: "AccountTypes",
                        principalColumn: "AccountTypeId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Accounts_UserGoogleSpreadsheets_UserGoogleSpreadsheetId",
                        column: x => x.UserGoogleSpreadsheetId,
                        principalTable: "UserGoogleSpreadsheets",
                        principalColumn: "UserGoogleSpreadsheetId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AccountBalanceHistories",
                columns: table => new
                {
                    AccountBalanceHistoryId = table.Column<Guid>(type: "uuid", nullable: false),
                    AccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    Balance = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    BalanceRecordedDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    SystemCreatedDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    SystemModifiedDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountBalanceHistories", x => x.AccountBalanceHistoryId);
                    table.ForeignKey(
                        name: "FK_AccountBalanceHistories_Accounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Accounts",
                        principalColumn: "AccountId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Transactions",
                columns: table => new
                {
                    TransactionId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<string>(type: "character varying(36)", maxLength: 36, nullable: false),
                    AccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    TransactionTypeId = table.Column<int>(type: "integer", nullable: false),
                    TransactionCategoryId = table.Column<int>(type: "integer", nullable: false),
                    Date = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Description = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    FinancialTransactionId = table.Column<long>(type: "bigint", nullable: false),
                    SystemCreatedDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    SystemModifiedDate = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true),
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
                    table.ForeignKey(
                        name: "FK_Transactions_TransactionCategories_TransactionCategoryId",
                        column: x => x.TransactionCategoryId,
                        principalTable: "TransactionCategories",
                        principalColumn: "TransactionCategoryId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Transactions_TransactionTypes_TransactionTypeId",
                        column: x => x.TransactionTypeId,
                        principalTable: "TransactionTypes",
                        principalColumn: "TransactionTypeId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AccountBalanceHistories_AccountId",
                table: "AccountBalanceHistories",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_Accounts_AccountTypeId",
                table: "Accounts",
                column: "AccountTypeId");

            migrationBuilder.CreateIndex(
                name: "IX_Accounts_UserGoogleSpreadsheetId",
                table: "Accounts",
                column: "UserGoogleSpreadsheetId");

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_AccountId",
                table: "Transactions",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_TransactionCategoryId",
                table: "Transactions",
                column: "TransactionCategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_TransactionTypeId",
                table: "Transactions",
                column: "TransactionTypeId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AccountBalanceHistories");

            migrationBuilder.DropTable(
                name: "Transactions");

            migrationBuilder.DropTable(
                name: "Accounts");

            migrationBuilder.DropTable(
                name: "TransactionCategories");

            migrationBuilder.DropTable(
                name: "TransactionTypes");

            migrationBuilder.DropTable(
                name: "AccountTypes");

            migrationBuilder.DropTable(
                name: "UserGoogleSpreadsheets");
        }
    }
}
