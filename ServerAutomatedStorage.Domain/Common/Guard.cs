using System;
using System.Collections.Generic;
using System.Text;

namespace ServerAutomatedStorage.Domain.Common
{
    public static class Guard
    {
        public static string AgainstNullOrWhiteSpace(string? value, string paramName)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new DomainException($"Поле {paramName} не заполнено.");
            return value;
        }

        public static T AgainstNull<T>(T? value, string paramName) where T : class
        {
            if (value is null)
                throw new DomainException($"Поле {paramName} не заполнено.");
            return value;
        }

        public static int AgainstNegativeOrZero(int value, string paramName)
        {
            if (value <= 0)
                throw new DomainException($"Значение {paramName} должно быть больше нуля.");
            return value;
        }

        public static double AgainstNegativeOrZero(double value, string paramName)
        {
            if (value <= 0)
                throw new DomainException($"Значение {paramName} должно быть больше нуля.");
            return value;
        }

        public static int AgainstNegative(int value, string paramName)
        {
            if (value < 0)
                throw new DomainException($"Значение {paramName} не может быть отрицательным.");
            return value;
        }

        public static double AgainstNegative(double value, string paramName)
        {
            if (value < 0)
                throw new DomainException($"Значение {paramName} не может быть отрицательным.");
            return value;
        }
        public static Guid AgainstDefault(Guid value, string paramName)
        {
            if (value == Guid.Empty)
                throw new DomainException($"Поле {paramName} не заполнено.");
            return value;
        }
    }
}
