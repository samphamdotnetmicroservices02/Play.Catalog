using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Logging;
using Microsoft.IdentityModel.Tokens;
using Play.Common.Settings;

namespace Play.Catalog.Service.Identity;

public class ConfigureJwtBearerOptions : IConfigureNamedOptions<JwtBearerOptions>
{
    //AccessTokenParameter is going to be the name of the parameter in the query string that we have to get access to, to retrieve that access
    //token from the request to use it later.
    private const string AccessTokenParameter = "access_token";

    //MessageHubPath is going to be the path where the client can access the Message Hub or the SignalR Hub that we will enable in our trading
    //microservice in the next lesson
    private const string MessageHubPath = "/messageHub";

    private readonly IConfiguration _configuration;

    public ConfigureJwtBearerOptions(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public void Configure(string name, JwtBearerOptions options)
    {
        if (name == JwtBearerDefaults.AuthenticationScheme)
        {
            var serviceSettings = _configuration.GetSection(nameof(ServiceSettings)).Get<ServiceSettings>();

            options.Authority = serviceSettings.Authority;
            options.Audience = serviceSettings.ServiceName;

            //Juse use for apllications run on local Kubernetes and not use HTTPS
            if (serviceSettings.IsKubernetesLocal.Equals("true", StringComparison.OrdinalIgnoreCase))
            {
                options.RequireHttpsMetadata = false;
                IdentityModelEventSource.ShowPII = true;
                System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12;
                //<service-name>.<namespace>.svc.cluster.local
                //https://github.com/IdentityServer/IdentityServer4/issues/2450
                options.MetadataAddress = "http://identity-service.identity.svc.cluster.local/.well-known/openid-configuration";
            }

            /*
            * if you don't do this, things will work just fine. However, you may find issues moving forward as you're
            * trying to integrate with other kinds of identity providers that use more standard claims that you may
            * not be using in your project. so with this, you're getting into the correct standards
            */
            options.MapInboundClaims = false;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                NameClaimType = "name",
                RoleClaimType = "role"
            };

            options.Events = new JwtBearerEvents
            {
                /* 
                * OnMessageReceived: what we are doing here is to configuring our authentication logic, our JwtBearer authentication logic,
                * so that anytime a message is received for authentication, and if we retrieve the access token from the request query string,
                * we figured out what's the path, and if that path is the Message Hub path, like /messageHub in this case, and only in that case,
                * and we'll go ahead and take that access token that we retrieved from the query string and copy it into this 
                * Message Received Context that SignalR's going to use.
                */
                OnMessageReceived = context =>
                {
                    var accessToken = context.Request.Query[AccessTokenParameter];
                    var path = context.HttpContext.Request.Path;

                    if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments(MessageHubPath))
                    {
                        context.Token = accessToken;
                    }

                    return Task.CompletedTask;
                }
            };
        }
    }

    /*
    * You're not going to be really be using this method here, however, it is good to provide some meaning ful 
    *    default. To what we're going to do is just have it call the other method with some default in name
    */
    public void Configure(JwtBearerOptions options)
    {
        Configure(Options.DefaultName, options);
    }
}