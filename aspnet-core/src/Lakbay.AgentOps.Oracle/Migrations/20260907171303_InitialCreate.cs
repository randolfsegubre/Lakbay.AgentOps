using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lakbay.AgentOps.Oracle.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CALL_RECORDS",
                columns: table => new
                {
                    ID = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    CALLER_PHONE_NUMBER = table.Column<string>(type: "NVARCHAR2(32)", maxLength: 32, nullable: false),
                    MATCHED_CUSTOMER_NAME = table.Column<string>(type: "NVARCHAR2(200)", maxLength: 200, nullable: true),
                    AGENT_ID = table.Column<Guid>(type: "RAW(16)", nullable: false),
                    CALL_START_UTC = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: false),
                    CALL_END_UTC = table.Column<DateTime>(type: "TIMESTAMP(7)", nullable: true),
                    OUTCOME = table.Column<int>(type: "NUMBER(10)", nullable: false),
                    BOOKING_ID = table.Column<Guid>(type: "RAW(16)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CALL_RECORDS", x => x.ID);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CALL_RECORDS_AGENT_ID",
                table: "CALL_RECORDS",
                column: "AGENT_ID");

            migrationBuilder.CreateIndex(
                name: "IX_CALL_RECORDS_CALL_START_UTC",
                table: "CALL_RECORDS",
                column: "CALL_START_UTC");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CALL_RECORDS");
        }
    }
}
