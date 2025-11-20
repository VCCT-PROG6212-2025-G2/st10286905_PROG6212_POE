using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations.Schema;

namespace ContractMonthlyClaimSystem.Models
{
    [Table("ClaimInvoiceDocument")]
    [PrimaryKey(nameof(ClaimId))]
    public class ClaimInvoiceDocument
    {
        [ForeignKey(nameof(Claim))]
        public int ClaimId { get; set; }
        public virtual ContractClaim? Claim { get; set; }

        [ForeignKey(nameof(UploadedFile))]
        public int UploadedFileId { get; set; }
        public virtual UploadedFile? UploadedFile { get; set; }
    }
}
