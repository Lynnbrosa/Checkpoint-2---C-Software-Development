using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ExpenseHub.Api.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ExpenseCategories",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 80, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExpenseCategories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Expenses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    OwnerId = table.Column<string>(type: "TEXT", maxLength: 450, nullable: false),
                    CategoryId = table.Column<int>(type: "INTEGER", nullable: false),
                    Description = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                    Amount = table.Column<decimal>(type: "TEXT", precision: 12, scale: 2, nullable: false),
                    ExpenseDate = table.Column<DateOnly>(type: "TEXT", nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    RejectionReason = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ConcurrencyStamp = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Expenses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Expenses_ExpenseCategories_CategoryId",
                        column: x => x.CategoryId,
                        principalTable: "ExpenseCategories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "ExpenseHistory",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ExpenseId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Action = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    ActorId = table.Column<string>(type: "TEXT", maxLength: 450, nullable: false),
                    OccurredAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    FromStatus = table.Column<string>(type: "TEXT", maxLength: 20, nullable: true),
                    ToStatus = table.Column<string>(type: "TEXT", maxLength: 20, nullable: false),
                    Justification = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    Changes = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExpenseHistory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExpenseHistory_Expenses_ExpenseId",
                        column: x => x.ExpenseId,
                        principalTable: "Expenses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "PaymentRecords",
                columns: table => new
                {
                    Id = table.Column<long>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ExpenseId = table.Column<Guid>(type: "TEXT", nullable: false),
                    Amount = table.Column<decimal>(type: "TEXT", precision: 12, scale: 2, nullable: false),
                    PaidById = table.Column<string>(type: "TEXT", maxLength: 450, nullable: false),
                    PaidAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentRecords", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PaymentRecords_Expenses_ExpenseId",
                        column: x => x.ExpenseId,
                        principalTable: "Expenses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                table: "ExpenseCategories",
                columns: new[] { "Id", "Name" },
                values: new object[,]
                {
                    { 1, "Transporte" },
                    { 2, "Alimentação" },
                    { 3, "Hospedagem" },
                    { 4, "Material de escritório" },
                    { 5, "Outros" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_ExpenseCategories_Name",
                table: "ExpenseCategories",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExpenseHistory_ExpenseId_OccurredAt",
                table: "ExpenseHistory",
                columns: new[] { "ExpenseId", "OccurredAt" });

            migrationBuilder.CreateIndex(
                name: "IX_Expenses_CategoryId",
                table: "Expenses",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Expenses_OwnerId",
                table: "Expenses",
                column: "OwnerId");

            migrationBuilder.CreateIndex(
                name: "IX_Expenses_Status",
                table: "Expenses",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentRecords_ExpenseId",
                table: "PaymentRecords",
                column: "ExpenseId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ExpenseHistory");

            migrationBuilder.DropTable(
                name: "PaymentRecords");

            migrationBuilder.DropTable(
                name: "Expenses");

            migrationBuilder.DropTable(
                name: "ExpenseCategories");
        }
    }
}
