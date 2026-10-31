using Microsoft.AspNetCore.Identity;

namespace ServerAutomatedStorage.Infrastructure.Identity
{
    public class ApplicationUser : IdentityUser<Guid>
    {
        public Guid DomainUserId { get; set; }
    }
}
