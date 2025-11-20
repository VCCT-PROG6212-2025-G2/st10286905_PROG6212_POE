using ContractMonthlyClaimSystem.Data;
using ContractMonthlyClaimSystem.Models;
using ContractMonthlyClaimSystem.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;

namespace ContractMonthlyClaimSystem.Services
{
    public class HumanResourcesService(
        AppDbContext context,
        IReviewerClaimService reviewerClaimService,
        IFileService fileService
    ) : IHumanResourcesService
    {
        private readonly AppDbContext _context = context;
        private readonly IReviewerClaimService _reviewerClaimService = reviewerClaimService;
        private readonly IFileService _fileService = fileService;

        public async Task<LecturerDetails?> GetLecturerDetailsAsync(int id) =>
            await _context.LecturerDetails.FindAsync(id);

        public async Task SetLecturerDetailsAsync(LecturerDetails details)
        {
            if (details == null)
                return;

            var existing = await _context.LecturerDetails.FirstOrDefaultAsync(x =>
                x.UserId == details.UserId
            );

            if (existing == null)
            {
                _context.LecturerDetails.Add(details);
            }
            else
            {
                existing.ContactNumber = details.ContactNumber;
                existing.Address = details.Address;
                existing.BankDetails = details.BankDetails;
            }
            await _context.SaveChangesAsync();
        }

        public async Task<int> AutoReviewClaimsForReviewersAsync()
        {
            var reviewed = 0;

            var reviewerIds = await _context
                .AutoReviewRules.Select(arr => arr.ReviewerId)
                .ToListAsync();

            foreach (var reviewerId in reviewerIds)
                reviewed += (await _reviewerClaimService.AutoReviewPendingClaimsAsync(reviewerId)).reviewed;

            return reviewed;
        }

        public async Task<List<ContractClaim>> GetApprovedClaimsAsync() =>
            await _context
                .ContractClaims.Include(c => c.LecturerUser)
                .Include(c => c.Module)
                .Include(c => c.AcademicManagerUser)
                .Include(c => c.ProgramCoordinatorUser)
                .Where(c => c.ClaimStatus == ClaimStatus.ACCEPTED)
                .ToListAsync();

        public async Task<int> ProcessApprovedClaimInvoicesAsync()
        {
            int ret = 0;

            // Filter claims
            var approvedClaims = await GetApprovedClaimsAsync();
            var unprocessedClaims = approvedClaims.Except(
                _context.ClaimInvoiceDocuments.Select(d => d.Claim)
            );
            if (!unprocessedClaims.Any())
                return ret; // Nothing to process ..

            foreach (var claim in unprocessedClaims)
            {
                var res = await GetClaimInvoicePdfAsync(claim!.Id);
                if (res != null)
                    ret++;
            }

            return ret;
        }

        public async Task<List<ClaimInvoiceDocument>> GetProcessedClaimInvoicesAsync() =>
            await _context
                .ClaimInvoiceDocuments.Include(d => d.Claim)
                .Include(d => d.UploadedFile)
                .ToListAsync();

        public async Task<(
            Stream FileStream,
            string ContentType,
            string FileName
        )?> GetClaimInvoicePdfAsync(int claimId)
        {
            var claim = await _reviewerClaimService.GetClaimAsync(claimId);
            if (claim == null)
                return null;

            var claimInvoice = await _context.ClaimInvoiceDocuments.FindAsync(claimId);
            if (claimInvoice != null) // Invoice exist? Immediately return the file.
                return await _fileService.GetFileAsync(claimInvoice.UploadedFileId);

            // Claim invoice does not yet exist, so generate it
            var file = await GenerateClaimInvoicePdfAsync(claimId);
            if (file == null)
                return null; // Something went wrong ..

            // Then save and track it
            var uploadedFile = await _fileService.UploadFileAsync(
                file.Value.FileStream,
                file.Value.FileName,
                file.Value.FileStream.Length
            );
            if (uploadedFile == null)
                return null; // Something went wrong ..

            // Add the ClaimInvoiceDocument entry
            _context.ClaimInvoiceDocuments.Add(
                new ClaimInvoiceDocument
                {
                    Claim = claim,
                    ClaimId = claimId,
                    UploadedFile = uploadedFile,
                    UploadedFileId = uploadedFile.Id,
                }
            );
            await _context.SaveChangesAsync();

            // Finally return the newly generated and saved file.
            return await _fileService.GetFileAsync(uploadedFile.Id);
        }

        public async Task<(
            Stream FileStream,
            string ContentType,
            string FileName
        )?> GenerateClaimInvoicePdfAsync(int claimId)
        {
            var claim = await _reviewerClaimService.GetClaimAsync(claimId);
            if (claim == null || claim.ClaimStatus != ClaimStatus.ACCEPTED)
                return null;

            var lecturerDetails = await GetLecturerDetailsAsync(claim.LecturerUserId);

            // AI Disclosure: ChatGPT assisted here. Link: https://chatgpt.com/share/691e471c-9684-800b-bac0-cb32ebf95348
            var lecturerName =
                $"{claim.LecturerUser.FirstName} {claim.LecturerUser.LastName}".Trim();

            decimal total = claim.HoursWorked * claim.HourlyRate;
            string formattedDate = DateTime.Now.ToString("yyyy-MM-dd");

            var stream = new MemoryStream();

            QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Size(QuestPDF.Helpers.PageSizes.A4);
                    page.Margin(40);

                    page.Header()
                        .Text("LECTURER CLAIM INVOICE")
                        .SemiBold()
                        .FontSize(20)
                        .AlignCenter();

                    page.Content()
                        .Column(col =>
                        {
                            col.Spacing(10);

                            col.Item().Text($"Invoice Date: {formattedDate}");
                            col.Item().Text($"Lecturer: {lecturerName}");
                            col.Item()
                                .Text($"Contact Number: {lecturerDetails?.ContactNumber ?? "N/A"}");
                            col.Item().Text($"Address: {lecturerDetails?.Address ?? "N/A"}");

                            col.Item().Text("Banking Information").SemiBold();
                            col.Item().PaddingLeft(10).Text(lecturerDetails?.BankDetails ?? "N/A");

                            col.Item().LineHorizontal(1);

                            col.Item().Text("Claim Details").SemiBold().FontSize(16);

                            col.Item()
                                .Table(table =>
                                {
                                    table.ColumnsDefinition(columns =>
                                    {
                                        columns.ConstantColumn(150);
                                        columns.RelativeColumn();
                                    });

                                    // Row: Module
                                    table.Cell().PaddingVertical(4).Text("Module").SemiBold();
                                    table.Cell().PaddingVertical(4).Text(claim.Module.Name);

                                    // Row: Hours Worked
                                    table
                                        .Cell()
                                        .PaddingVertical(4)
                                        .Text("Hours Worked")
                                        .SemiBold();
                                    table.Cell().PaddingVertical(4).Text($"{claim.HoursWorked}");

                                    // Row: Hourly Rate
                                    table
                                        .Cell()
                                        .PaddingVertical(4)
                                        .Text("Hourly Rate (R)")
                                        .SemiBold();
                                    table.Cell().PaddingVertical(4).Text($"{claim.HourlyRate:F2}");

                                    // Row: Total
                                    table.Cell().PaddingVertical(4).Text("Total (R)").SemiBold();
                                    table.Cell().PaddingVertical(4).Text($"{total:F2}");
                                });

                            col.Item().LineHorizontal(1);

                            if (!string.IsNullOrWhiteSpace(claim.LecturerComment))
                            {
                                col.Item().Text("Lecturer Comment:").SemiBold();
                                col.Item().Text(claim.LecturerComment);
                            }

                            col.Item().Text("Program Coordinator Decision:").SemiBold();
                            col.Item().Text($"{claim.ProgramCoordinatorDecision}");
                            if (!string.IsNullOrWhiteSpace(claim.ProgramCoordinatorComment))
                            {
                                col.Item().Text("Program Coordinator Comment:").SemiBold();
                                col.Item().Text(claim.ProgramCoordinatorComment);
                            }

                            col.Item().Text("Academic Manager Decision:").SemiBold();
                            col.Item().Text($"{claim.AcademicManagerDecision}");
                            if (!string.IsNullOrWhiteSpace(claim.AcademicManagerComment))
                            {
                                col.Item().Text("Academic Manager Comment:").SemiBold();
                                col.Item().Text(claim.AcademicManagerComment);
                            }
                        });

                    page.Footer()
                        .AlignCenter()
                        .Text(txt =>
                        {
                            txt.Span("Generated by ContractMonthlyClaimSystem — ");
                            txt.Span(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")).Italic();
                        });
                });
            });

            document.GeneratePdf(stream);
            stream.Position = 0;

            return (
                FileStream: stream,
                ContentType: "application/pdf",
                FileName: $"ClaimInvoice_{claim.Id}.pdf"
            );
        }
    }
}
