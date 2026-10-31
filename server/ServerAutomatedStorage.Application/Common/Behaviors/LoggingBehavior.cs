using MediatR;
using Microsoft.Extensions.Logging;
using ServerAutomatedStorage.Application.Common.Interfaces;

namespace ServerAutomatedStorage.Application.Common.Behaviors
{
    public sealed class LoggingBehavior<TRequest, TResponse>(
        ILogger<LoggingBehavior<TRequest, TResponse>> logger,
        ICurrentUser currentUser) : IPipelineBehavior<TRequest, TResponse>
        where TRequest : notnull
    {
        public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        {
            var response = await next(cancellationToken);

            var name = typeof(TRequest).Name;
            if (name.EndsWith("Command", StringComparison.Ordinal) && currentUser.UserId is { } userId && logger.IsEnabled(LogLevel.Information))
                logger.LogInformation("Пользователь {UserId} выполнил {Command}", userId, name);

            return response;
        }
    }
}
