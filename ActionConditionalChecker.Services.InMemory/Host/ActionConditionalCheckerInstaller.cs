using ActionConditionalChecker.Contracts;
using Microsoft.Extensions.DependencyInjection;

namespace ActionConditionalChecker.Services.InMemory.Host
{
    public static class ActionConditionalCheckerInstaller
    {
        public static IServiceCollection AddActionConditionalCheckerInstaller(this IServiceCollection services)
        {
            services.AddTransient<IActionConditionalChecker, ActionConditionalChecker>();
            services.AddTransient<IAsyncActionConditionalChecker, AsyncActionConditionalChecker>();

            return services;
        }
    }
}
