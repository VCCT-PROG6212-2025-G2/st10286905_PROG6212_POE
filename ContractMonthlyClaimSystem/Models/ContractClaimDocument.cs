using Microsoft.EntityFrameworkCore;

namespace ContractMonthlyClaimSystem.Models
{
    [PrimaryKey(nameof(ContractClaimId), nameof(UploadedFileId))]
    public class ContractClaimDocument
    {
        public int ContractClaimId { get; set; }
        public virtual ContractClaim ContractClaim { get; set; }
        public int UploadedFileId { get; set; }
        public virtual UploadedFile UploadedFile { get; set; }
    }
}
