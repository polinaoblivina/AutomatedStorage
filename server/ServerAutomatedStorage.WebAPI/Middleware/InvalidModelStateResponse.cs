using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace ServerAutomatedStorage.WebAPI.Middleware
{
    public static class InvalidModelStateResponse
    {
        private const string InvalidValueMessage = "Неверное значение поля.";

        public static IActionResult Create(ActionContext context)
        {
            var sanitized = new ModelStateDictionary();

            foreach (var (key, entry) in context.ModelState)
            {
                var isBodyPath = key.StartsWith('$');
                var field = key.StartsWith("$.", StringComparison.Ordinal) ? key[2..] : key;

                foreach (var error in entry.Errors)
                {
                    sanitized.AddModelError(field, isBodyPath || error.Exception is not null ? InvalidValueMessage : error.ErrorMessage);
                }
            }

            var factory = context.HttpContext.RequestServices.GetRequiredService<ProblemDetailsFactory>();
            var problem = factory.CreateValidationProblemDetails(
                context.HttpContext, sanitized, StatusCodes.Status400BadRequest, "Ошибка проверки данных");

            return new BadRequestObjectResult(problem);
        }
    }
}
