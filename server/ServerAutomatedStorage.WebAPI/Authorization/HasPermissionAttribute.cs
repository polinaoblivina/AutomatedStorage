using Microsoft.AspNetCore.Authorization;
using ServerAutomatedStorage.Domain.Enums;

namespace ServerAutomatedStorage.WebAPI.Authorization
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
    public sealed class HasPermissionAttribute(Permission permission)
    : AuthorizeAttribute(AuthorizationSetup.PolicyFor(permission));
}
