using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;

namespace Play.Catalog.Service.Identity;

public static class Extensions
{
    public static AuthenticationBuilder AddJwtBearerAuthenticationCatalog(this IServiceCollection services)
    {
        return services.ConfigureOptions<ConfigureJwtBearerOptions>()
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer();
    }
}