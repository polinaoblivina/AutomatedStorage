using System.IdentityModel.Tokens.Jwt;

namespace ServerAutomatedStorage.Infrastructure.Authentication
{
    public static class AuthClaims
    {
        public const string Subject = JwtRegisteredClaimNames.Sub;
        public const string Name = JwtRegisteredClaimNames.Name;
        public const string Role = "role";
        public const string Permission = "permission";
        public const string Device = "device";
    }
}
