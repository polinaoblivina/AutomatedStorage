using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using ServerAutomatedStorage.Application.Common.Behaviors;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text;

namespace ServerAutomatedStorage.Application
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddApplication(this IServiceCollection services)
        {
            var assembly = Assembly.GetExecutingAssembly();
            services.AddMediatR(cfg => 
                {
                    cfg.RegisterServicesFromAssembly(assembly);
                    cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
                });
            ValidatorOptions.Global.LanguageManager.Culture = new CultureInfo("ru");
            services.AddValidatorsFromAssembly(assembly);

            return services;
        }
    }
}
