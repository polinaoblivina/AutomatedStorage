using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.AspNetCore.Identity;

namespace ServerAutomatedStorage.Infrastructure.Identity
{
    public sealed class RussianIdentityErrorDescriber : IdentityErrorDescriber
    {
        public override IdentityError DuplicateEmail(string email) =>
            new() { Code = nameof(DuplicateEmail), Description = $"Email {email} уже используется." };

        public override IdentityError DuplicateUserName(string userName) =>
            new() { Code = nameof(DuplicateUserName), Description = $"Логин {userName} уже занят." };

        public override IdentityError InvalidEmail(string? email) =>
            new() { Code = nameof(InvalidEmail), Description = $"Email {email} указан некорректно." };

        public override IdentityError PasswordMismatch() =>
            new() { Code = nameof(PasswordMismatch), Description = "Неверный пароль." };

        public override IdentityError PasswordTooShort(int length) =>
            new() { Code = nameof(PasswordTooShort), Description = $"Пароль должен содержать не менее {length} символов." };

        public override IdentityError InvalidToken() =>
            new() { Code = nameof(InvalidToken), Description = "Недействительный токен." };
    }
}
