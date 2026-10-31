using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using ServerAutomatedStorage.Application;
using ServerAutomatedStorage.Application.Common.Interfaces;
using ServerAutomatedStorage.Application.Features.Auth.Commands;
using ServerAutomatedStorage.Application.Features.Items.Commands;
using ServerAutomatedStorage.Application.Features.Users.Commands;
using ServerAutomatedStorage.Domain.Interfaces;

namespace ServerAutomatedStorage.UnitTests.Regression
{
    [Trait("Category", "Regression")]
    public class DefectRegressionTests
    {
        private readonly IItemRepository _items = Substitute.For<IItemRepository>();
        private readonly ISender _sender;

        public DefectRegressionTests()
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddApplication();
            services.AddSingleton(Substitute.For<ICurrentUser>());
            services.AddSingleton(_items);
            services.AddSingleton(Substitute.For<ITransactionRepository>());
            services.AddSingleton(Substitute.For<IUnitOfWork>());
            _sender = services.BuildServiceProvider().GetRequiredService<ISender>();
        }

        [Fact]
        [Trait("Defect", "D-1")]
        public async Task Replenish_AboveLimit_IsRejectedBeforeHandler()
        {
            var command = new ReplenishStockCommand(Guid.NewGuid(), 100_001, Guid.NewGuid());

            await Assert.ThrowsAsync<ValidationException>(() => _sender.Send(command));

            await _items.DidNotReceiveWithAnyArgs().GetByIdAsync(default, default);
        }

        [Fact]
        [Trait("Defect", "D-2")]
        public async Task Adjust_AboveLimit_IsRejected()
        {
            var command = new AdjustStockCommand(Guid.NewGuid(), 1_000_001, Guid.NewGuid());

            await Assert.ThrowsAsync<ValidationException>(() => _sender.Send(command));
        }

        [Fact]
        [Trait("Defect", "D-3")]
        public async Task ChangePassword_SameAsCurrent_IsRejected()
        {
            var command = new ChangePasswordCommand(Guid.NewGuid(), "secret1", "secret1");

            await Assert.ThrowsAsync<ValidationException>(() => _sender.Send(command));
        }

        [Fact]
        [Trait("Defect", "D-4")]
        public async Task AssignCard_TooLong_IsRejected()
        {
            var command = new AssignCardCommand(Guid.NewGuid(), new string('X', 51));

            await Assert.ThrowsAsync<ValidationException>(() => _sender.Send(command));
        }

        [Fact]
        [Trait("Defect", "D-4")]
        public async Task UpdateUser_NameTooLong_IsRejected()
        {
            var command = new UpdateUserCommand(Guid.NewGuid(), new string('N', 201));

            await Assert.ThrowsAsync<ValidationException>(() => _sender.Send(command));
        }
    }
}
