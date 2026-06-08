using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BudgetTracker.Api.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddPayslips : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Payslips",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Country = table.Column<int>(type: "integer", nullable: false),
                    EmployerName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    EmployeeName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    PersonnummerEncrypted = table.Column<string>(type: "character varying(1024)", maxLength: 1024, nullable: true),
                    PersonnummerMasked = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    PayPeriodStart = table.Column<DateOnly>(type: "date", nullable: false),
                    PayPeriodEnd = table.Column<DateOnly>(type: "date", nullable: false),
                    PayDate = table.Column<DateOnly>(type: "date", nullable: false),
                    CurrencyCode = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false, defaultValue: "SEK"),
                    DeclaredNet = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Notes = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    PostedTransactionId = table.Column<Guid>(type: "uuid", nullable: true),
                    PostedAccountId = table.Column<Guid>(type: "uuid", nullable: true),
                    PostedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    OwnerUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Visibility = table.Column<int>(type: "integer", nullable: false),
                    HouseholdId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Payslips", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PayslipLeaveBalances",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PayslipId = table.Column<Guid>(type: "uuid", nullable: false),
                    LeaveType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Balance = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    Unit = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false, defaultValue: "days"),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PayslipLeaveBalances", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PayslipLeaveBalances_Payslips_PayslipId",
                        column: x => x.PayslipId,
                        principalTable: "Payslips",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PayslipLineItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PayslipId = table.Column<Guid>(type: "uuid", nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    Label = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Quantity = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    UnitAmount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: true),
                    Amount = table.Column<decimal>(type: "numeric(18,2)", precision: 18, scale: 2, nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PayslipLineItems", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PayslipLineItems_Payslips_PayslipId",
                        column: x => x.PayslipId,
                        principalTable: "Payslips",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PayslipLeaveBalances_PayslipId",
                table: "PayslipLeaveBalances",
                column: "PayslipId");

            migrationBuilder.CreateIndex(
                name: "IX_PayslipLineItems_PayslipId",
                table: "PayslipLineItems",
                column: "PayslipId");

            migrationBuilder.CreateIndex(
                name: "IX_Payslips_HouseholdId",
                table: "Payslips",
                column: "HouseholdId");

            migrationBuilder.CreateIndex(
                name: "IX_Payslips_OwnerUserId",
                table: "Payslips",
                column: "OwnerUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Payslips_OwnerUserId_PayDate",
                table: "Payslips",
                columns: new[] { "OwnerUserId", "PayDate" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PayslipLeaveBalances");

            migrationBuilder.DropTable(
                name: "PayslipLineItems");

            migrationBuilder.DropTable(
                name: "Payslips");
        }
    }
}
