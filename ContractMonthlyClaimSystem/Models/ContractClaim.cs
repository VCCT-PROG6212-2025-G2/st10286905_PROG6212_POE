namespace ContractMonthlyClaimSystem.Models
{
    public class ContractClaim
    {
        public int Id { get; set; }

        public string LecturerUserId { get; set; }
        public virtual AppUser LecturerUser { get; set; }

        public int ModuleId { get; set; }
        public virtual Module Module { get; set; }

        public decimal HoursWorked { get; set; }
        public decimal HourlyRate { get; set; }
        public string? LecturerComment { get; set; }

        public string? ProgramCoordinatorUserId { get; set; }
        public virtual AppUser? ProgramCoordinatorUser { get; set; }
        public ClaimDecision ProgramCoordinatorDecision { get; set; } = ClaimDecision.PENDING;
        public string? ProgramCoordinatorComment { get; set; }

        public string? AcademicManagerUserId { get; set; }
        public virtual AppUser? AcademicManagerUser { get; set; }
        public ClaimDecision AcademicManagerDecision { get; set; } = ClaimDecision.PENDING;
        public string? AcademicManagerComment { get; set; }

        public ClaimStatus ClaimStatus { get; set; } = ClaimStatus.PENDING;
    }
}
