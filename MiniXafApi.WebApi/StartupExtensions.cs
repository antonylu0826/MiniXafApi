using DevExpress.ExpressApp.ApplicationBuilder;
using MiniXafApi.WebApi.API.Security;
using System.ComponentModel;

namespace Microsoft.Extensions.DependencyInjection;

[Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
public static class StartupExtensions
{
    public static IAspNetCoreSecurityBuilder<TBuilder> AddRopcAuthentication<TBuilder>(this IAspNetCoreSecurityBuilder<TBuilder> builder, Action<RopcOptions>? options = null) where TBuilder : IAspNetCoreApplicationBuilder<TBuilder>
    {
        var services = builder.Context.ServerConfiguration.Services;
        services.Configure(options);
        services.AddHttpClient();
        services.AddScoped<RopcTokenProviderService, RopcTokenProviderService>();
        builder.AddAuthenticationProvider<RopcAuthenticationProvider>();
        return builder;
    }
}