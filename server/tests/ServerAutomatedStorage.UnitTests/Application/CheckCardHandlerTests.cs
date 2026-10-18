using NSubstitute;
using ServerAutomatedStorage.Application.Features.Access.Commands;
using ServerAutomatedStorage.Application.Features.Access.DTOs;
using ServerAutomatedStorage.Domain.Entities;
using ServerAutomatedStorage.Domain.Enums;
using ServerAutomatedStorage.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace ServerAutomatedStorage.UnitTests.Application
{
    public class CheckCardHandlerTests
    {
        private const string Card = "A1B2C3D4";

        private readonly IUserRepository _users = Substitute.For<IUserRepository>();
        private readonly CheckCardHandler _handler;

        public CheckCardHandlerTests()
        {
            _handler = new CheckCardHandler(_users);
        }

        private void GivenUser(User? user) =>
            _users.GetByCardUidAsync(Card, Arg.Any<CancellationToken>()).Returns(user);

        [Fact]
        public async Task UnknownCard_IsDenied()
        {
            GivenUser(null);

            var response = await _handler.Handle(new CheckCardCommand(Card), CancellationToken.None);

            Assert.False(response.AccessGranted);
            Assert.Equal(CardCheckResult.UnknownCard, response.Result);
        }

        [Fact]
        public async Task InactiveUser_IsDenied()
        {
            var user = new User("Иванов И.И.", new Role("Employee", Permission.OpenCell), Card);
            user.Deactivate();
            GivenUser(user);

            var response = await _handler.Handle(new CheckCardCommand(Card), CancellationToken.None);

            Assert.False(response.AccessGranted);
            Assert.Equal(CardCheckResult.UserInactive, response.Result);
        }

        [Fact]
        public async Task UserWithoutOpenCell_IsDenied()
        {
            GivenUser(new User("Иванов И.И.", new Role("Guest", Permission.None), Card));

            var response = await _handler.Handle(new CheckCardCommand(Card), CancellationToken.None);

            Assert.Equal(CardCheckResult.NoPermission, response.Result);
        }

        [Fact]
        public async Task ActiveUserWithOpenCell_IsGranted()
        {
            GivenUser(new User("Иванов И.И.", new Role("Employee", Permission.OpenCell), Card));

            var response = await _handler.Handle(new CheckCardCommand(Card), CancellationToken.None);

            Assert.True(response.AccessGranted);
            Assert.Equal("Иванов И.И.", response.UserName);
        }
    }
}
