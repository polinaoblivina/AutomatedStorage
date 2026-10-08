using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using ServerAutomatedStorage.Application.Common.Exceptions;
using ServerAutomatedStorage.Domain.Common;

namespace ServerAutomatedStorage.WebAPI.Middleware
{
    public sealed class GlobalExceptionHandler(
        IProblemDetailsService problemDetailsService,
        ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
    {
        public async ValueTask<bool> TryHandleAsync(
            HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
        {
            var problem = exception switch
            {
                ValidationException ex => CreateValidationProblem(ex),
                NotFoundException ex => Problem(StatusCodes.Status404NotFound, "Не найдено", ex.Message),
                ConflictException ex => Problem(StatusCodes.Status409Conflict, "Конфликт", ex.Message),
                UnauthorizedException ex => Problem(StatusCodes.Status401Unauthorized, "Не авторизован", ex.Message),
                DomainException ex => Problem(StatusCodes.Status400BadRequest, "Нарушение бизнес-правила", ex.Message),
                DbUpdateException { InnerException: PostgresException { SqlState: PostgresErrorCodes.UniqueViolation } }
                    => Problem(StatusCodes.Status409Conflict, "Конфликт", "Запись с таким значением уже существует."),
                OperationCanceledException when httpContext.RequestAborted.IsCancellationRequested
                    => null,
                _ => Problem(StatusCodes.Status500InternalServerError, "Ошибка сервера", "Произошла непредвиденная ошибка.")
            };

            if (problem is null)
            {
                httpContext.Response.StatusCode = 499;
                return true;
            }

            if (problem.Status >= 500)
                logger.LogError(exception, "Unhandled exception for {Method} {Path}",
                    httpContext.Request.Method, httpContext.Request.Path);
            else
                logger.LogInformation("Request failed with {Status}: {Message}", problem.Status, exception.Message);

            httpContext.Response.StatusCode = problem.Status!.Value;

            return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
            {
                HttpContext = httpContext,
                ProblemDetails = problem,
                Exception = exception
            });
        }

        private static ProblemDetails Problem(int status, string title, string detail) =>
            new() { Status = status, Title = title, Detail = detail };

        private static ValidationProblemDetails CreateValidationProblem(ValidationException ex)
        {
            var errors = ex.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).Distinct().ToArray());

            return new ValidationProblemDetails(errors)
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Ошибка проверки данных",
                Detail = string.Join(" ", errors.Values.SelectMany(m => m).Distinct())
            };
        }
    }
}