// AI Disclosure: ChatGPT assisted in creating this. Link: https://chatgpt.com/share/68f3b4ed-3c8c-800b-a26a-d00d7f3b3409
using ContractMonthlyClaimSystem.Services.Interfaces;

namespace ContractMonthlyClaimSystem.Services
{
    public static class ServiceRegistration
    {
        public static void AddApplicationServices(this IServiceCollection services)
        {
            services.AddScoped<IFileEncryptionService, FileEncryptionService>();
            services.AddScoped<ILecturerClaimService, LecturerClaimService>();
            services.AddScoped<IReviewerClaimService, ReviewerClaimService>();
            services.AddScoped<IModuleService, ModuleService>();
            services.AddScoped<IRoleService, RoleService>();
        }
    }
}
