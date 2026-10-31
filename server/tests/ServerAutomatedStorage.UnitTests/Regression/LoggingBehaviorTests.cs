using MediatR;
using Microsoft.Extensions.Logging;
using NSubstitute;
using ServerAutomatedStorage.Application.Common.Behaviors;
using ServerAutomatedStorage.Application.Common.Interfaces;
using ServerAutomatedStorage.Application.Features.Auth.Commands;
using ServerAutomatedStorage.Application.Features.Items.Commands;
using ServerAutomatedStorage.Application.Features.Transactions.Queries;

namespace ServerAutomatedStorage.UnitTests.Regression
{
    [Trait("Category", "Regression")]
    [Trait("Defect", "D-8")]
    public class LoggingBehaviorTests
    {
        private readonly ICurrentUser _currentUser = Substitute.For<ICurrentUser>();

        private sealed class ListLogger<T> : ILogger<T>
        {
            public List<string> Messages { get; } = [];

            public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

            public bool IsEnabled(LogLevel logLevel) => true;

            public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
                Messages.Add(formatter(state, exception));
        }

        private static Task<Unit> Next(CancellationToken _ = default) => Task.FromResult(Unit.Value);

        [Fact]
        public async Task Command_FromAuthenticatedUser_IsLoggedWithUserAndName()
        {
            var userId = Guid.NewGuid();
            _currentUser.UserId.Returns(userId);
            var logger = new ListLogger<LoggingBehavior<ReplenishStockCommand, Unit>>();
            var behavior = new LoggingBehavior<ReplenishStockCommand, Unit>(logger, _currentUser);

            await behavior.Handle(new ReplenishStockCommand(Guid.NewGuid(), 5, userId), Next, CancellationToken.None);

            var message = Assert.Single(logger.Messages);
            Assert.Contains(userId.ToString(), message);
            Assert.Contains(nameof(ReplenishStockCommand), message);
        }

        [Fact]
        public async Task Query_IsNotLogged()
        {
            _currentUser.UserId.Returns(Guid.NewGuid());
            var logger = new ListLogger<LoggingBehavior<GetTransactionsQuery, Unit>>();
            var behavior = new LoggingBehavior<GetTransactionsQuery, Unit>(logger, _currentUser);

            await behavior.Handle(new GetTransactionsQuery(), Next, CancellationToken.None);

            Assert.Empty(logger.Messages);
        }

        [Fact]
        public async Task Command_WithoutUser_IsNotLogged()
        {
            _currentUser.UserId.Returns((Guid?)null);
            var logger = new ListLogger<LoggingBehavior<ReplenishStockCommand, Unit>>();
            var behavior = new LoggingBehavior<ReplenishStockCommand, Unit>(logger, _currentUser);

            await behavior.Handle(new ReplenishStockCommand(Guid.NewGuid(), 5, Guid.NewGuid()), Next, CancellationToken.None);

            Assert.Empty(logger.Messages);
        }

        [Fact]
        public async Task PasswordCommand_LogMessageDoesNotContainPasswords()
        {
            _currentUser.UserId.Returns(Guid.NewGuid());
            var logger = new ListLogger<LoggingBehavior<ChangePasswordCommand, Unit>>();
            var behavior = new LoggingBehavior<ChangePasswordCommand, Unit>(logger, _currentUser);

            await behavior.Handle(new ChangePasswordCommand(Guid.NewGuid(), "OldSecret1", "NewSecret2"), Next, CancellationToken.None);

            var message = Assert.Single(logger.Messages);
            Assert.DoesNotContain("OldSecret1", message);
            Assert.DoesNotContain("NewSecret2", message);
        }
    }
}
