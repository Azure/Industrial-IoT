using Microsoft.Extensions.DependencyInjection;

namespace Blazored.SessionStorage
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddBlazoredSessionStorage(this IServiceCollection services)
        {
            return services
                .AddScoped<ISessionStorageService, SessionStorageService>()
                .AddScoped<ISyncSessionStorageService, SessionStorageService>();
        }
    }
}
