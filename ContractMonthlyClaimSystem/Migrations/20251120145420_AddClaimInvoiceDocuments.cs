using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ContractMonthlyClaimSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddClaimInvoiceDocuments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ClaimInvoiceDocument",
                columns: table => new
                {
                    ClaimId = table.Column<int>(type: "int", nullable: false),
                    UploadedFileId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClaimInvoiceDocument", x => x.ClaimId);
                    table.ForeignKey(
                        name: "FK_ClaimInvoiceDocument_ContractClaims_ClaimId",
                        column: x => x.ClaimId,
                        principalTable: "ContractClaims",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ClaimInvoiceDocument_UploadedFiles_UploadedFileId",
                        column: x => x.UploadedFileId,
                        principalTable: "UploadedFiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ClaimInvoiceDocument_UploadedFileId",
                table: "ClaimInvoiceDocument",
                column: "UploadedFileId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ClaimInvoiceDocument");
        }
    }
}
