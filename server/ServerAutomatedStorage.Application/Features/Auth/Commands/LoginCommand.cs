using FluentValidation;
using MediatR;
using ServerAutomatedStorage.Application.Common.Exceptions;
using ServerAutomatedStorage.Application.Common.Interfaces;
using ServerAutomatedStorage.Application.Features.Auth.DTOs;
using ServerAutomatedStorage.Application.Features.Users.DTOs;
using ServerAutomatedStorage.Domain.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace ServerAutomatedStorage.Application.Features.Auth.Commands
{
    public sealed record LoginCommand(string Email, string Password) : IRequest<LoginResponse>;

    public sealed class LoginValidator : AbstractValidator<LoginCommand>
    {
        public LoginValidator()
        {
            RuleFor(x => x.Email).NotEmpty().EmailAddress();
            RuleFor(x => x.Password).NotEmpty();
        }
    }

    public sealed class LoginHandler(
        IIdentityService identityService,
        IUserRepository userRepository,
        IJwtTokenGenerator tokenGenerator) : IRequestHandler<LoginCommand, LoginResponse>
    {
        public async Task<LoginResponse> Handle(LoginCommand request, CancellationToken cancellationToken)
        {
            var domainUserId = await identityService.ValidateCredentialsAsync(request.Email, request.Password)
                ?? throw new UnauthorizedException("Неверный email или пароль.");

            var user = await userRepository.GetByIdWithRoleAsync(domainUserId, cancellationToken);
            if (user is null || !user.IsActive)
                throw new UnauthorizedException("Учётная запись отключена.");

            var token = tokenGenerator.GenerateToken(user);

            return new LoginResponse(token.Token, token.ExpiresAtUtc, UserDTO.From(user, request.Email));
        }
    }
}

