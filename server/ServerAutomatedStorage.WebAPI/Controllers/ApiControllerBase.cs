using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace ServerAutomatedStorage.WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public abstract class ApiControllerBase : ControllerBase
    {
        protected ISender Sender => field ??= HttpContext.RequestServices.GetRequiredService<ISender>();
    }
}
