using DevExpress.ExpressApp;
using DevExpress.ExpressApp.ApplicationBuilder;
using DevExpress.ExpressApp.Security;
using DevExpress.ExpressApp.WebApi.Services;
using DevExpress.Persistent.BaseImpl.PermissionPolicy;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.OData;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using MiniXafApi.WebApi.API.Security;
using MiniXafApi.WebApi.BusinessObjects;
using MiniXafApi.WebApi.JWT;
using System.IdentityModel.Tokens.Jwt;
using System.Text;

namespace MiniXafApi.WebApi;

public class Startup
{
    public Startup(IConfiguration configuration)
    {
        Configuration = configuration;
    }

    public IConfiguration Configuration { get; }

    public void ConfigureServices(IServiceCollection services)
    {
        services.AddScoped<JwtTokenProviderService>();

        services.AddXafWebApi(builder =>
        {
            builder.AddXpoServices();

            builder.ConfigureOptions(options =>
            {
                options.BusinessObject<Employee>();
            });

            builder.Modules

                .Add<MiniXafApi.WebApi.MiniXafApiModule>();

            builder.ObjectSpaceProviders
                .AddSecuredXpo((serviceProvider, options) =>
                {
                    string connectionString = null;
                    if (Configuration.GetConnectionString("ConnectionString") != null)
                    {
                        connectionString = Configuration.GetConnectionString("ConnectionString");
                    }
                    ArgumentNullException.ThrowIfNull(connectionString);
                    options.ConnectionString = connectionString;
                    options.ThreadSafe = true;
                    options.UseSharedDataStoreProvider = true;
                })
                .AddNonPersistent();

            builder.Security
                .UseIntegratedMode(options =>
                {
                    options.Lockout.Enabled = true;

                    options.RoleType = typeof(PermissionPolicyRole);
                    options.UserType = typeof(ApplicationUser);
                    options.UserLoginInfoType = typeof(ApplicationUserLoginInfo);
                    options.UseXpoPermissionsCaching();
                    options.Events.OnSecurityStrategyCreated += securityStrategy =>
                    {
                        ((SecurityStrategy)securityStrategy).PermissionsReloadMode = PermissionsReloadMode.CacheOnFirstAccess;
                    };
                })
                .AddPasswordAuthentication(options =>
                {
                    options.IsSupportChangePassword = true;
                })
                .AddAuthenticationProvider<RopcAuthenticationProvider>()
                .AddRopcAuthentication(options =>
                {
                    options.TokenEndpoint = $"{Configuration["Authentication:Keycloak:Authority"]}/protocol/openid-connect/token";
                    options.ClientId = Configuration["Authentication:Keycloak:ClientId"];
                    options.ClientSecret = Configuration["Authentication:Keycloak:ClientSecret"];
                })
                ;


            builder.AddBuildStep(application =>
            {
                application.ApplicationName = "SetupApplication.MiniXafApi";
                application.CheckCompatibilityType = DevExpress.ExpressApp.CheckCompatibilityType.DatabaseSchema;
#if DEBUG
                if (System.Diagnostics.Debugger.IsAttached && application.CheckCompatibilityType == CheckCompatibilityType.DatabaseSchema)
                {
                    application.DatabaseUpdateMode = DatabaseUpdateMode.UpdateDatabaseAlways;
                    application.DatabaseVersionMismatch += (s, e) =>
                    {
                        e.Updater.Update();
                        e.Handled = true;
                    };
                }
#endif
            });
        }, Configuration);

        services
            .AddControllers()
            .AddOData((options, serviceProvider) =>
            {
                options
                    .AddRouteComponents("api/odata", new EdmModelBuilder(serviceProvider).GetEdmModel())
                    .EnableQueryFeatures(100);
            });

        services.AddAuthentication(options =>
            {
                options.DefaultScheme = "JwtSchemaSelector";
            })
            .AddPolicyScheme("JwtSchemaSelector", JwtBearerDefaults.AuthenticationScheme, options =>
             {
                 options.ForwardDefaultSelector = context =>
                 {
                     var authHeader = context.Request.Headers.Authorization.ToString();
                     if (authHeader?.StartsWith("Bearer ") == true)
                     {
                         var token = authHeader.Substring("Bearer ".Length);

                         try
                         {
                             var handler = new JwtSecurityTokenHandler();
                             var jwt = handler.ReadJwtToken(token);
                             var issuer = jwt.Issuer;
                             if (!string.IsNullOrEmpty(issuer))
                             {
                                 if (issuer.Contains("my-realm"))
                                     return "ROPC";
                             }
                         }
                         catch { }
                     }
                     return "DefaultJwt";
                 };
             })
            .AddJwtBearer("DefaultJwt", options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters()
                {
                    ValidateIssuerSigningKey = true,
                    //ValidIssuer = Configuration["Authentication:Jwt:Issuer"],
                    //ValidAudience = Configuration["Authentication:Jwt:Audience"],
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Configuration["Authentication:Jwt:IssuerSigningKey"]))
                };
            })
            .AddJwtBearer("ROPC", options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = Configuration["Authentication:Keycloak:Authority"],

                    ValidateAudience = true,
                    ValidAudience = "account",

                    ValidateIssuerSigningKey = true,
                    ValidateLifetime = false,

                    IssuerSigningKeyResolver = (token, securityToken, kid, parameters) =>
                    {
                        var client = new HttpClient();
                        var keyUri = $"{parameters.ValidIssuer}/protocol/openid-connect/certs";
                        var response = client.GetAsync(keyUri).Result;
                        var keys = new JsonWebKeySet(response.Content.ReadAsStringAsync().Result);

                        return keys.GetSigningKeys();
                    }
                };

                options.RequireHttpsMetadata = false; // Only in develop environment
                options.SaveToken = false;
            });

        services.AddAuthorization(options =>
        {
            options.DefaultPolicy = new AuthorizationPolicyBuilder("JwtSchemaSelector")
                    .RequireAuthenticatedUser()
                    .RequireXafAuthentication()
                    .Build();
        });

        services.AddSwaggerGen(c =>
        {
            c.EnableAnnotations();
            c.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "MiniXafApi API",
                Version = "v1",
                Description = @"Use AddXafWebApi(options) in the MiniXafApi.WebApi\Startup.cs file to make Business Objects available in the Web API."
            });
            c.AddSecurityDefinition("JWT", new OpenApiSecurityScheme()
            {
                Type = SecuritySchemeType.Http,
                Name = "Bearer",
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header
            });
            c.AddSecurityRequirement(new OpenApiSecurityRequirement() {
                {
                    new OpenApiSecurityScheme() {
                        Reference = new OpenApiReference() {
                            Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                            Id = "JWT"
                        }
                    },
                    new string[0]
                },
            });
        });

        services.Configure<Microsoft.AspNetCore.Mvc.JsonOptions>(o =>
        {
            o.JsonSerializerOptions.PropertyNamingPolicy = null;
        });
    }

    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
        if (env.IsDevelopment())
        {
            app.UseDeveloperExceptionPage();
            app.UseSwagger();
            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "MiniXafApi WebApi v1");
            });
        }
        else
        {
            app.UseExceptionHandler("/Error");
            app.UseHsts();
        }
        app.UseHttpsRedirection();
        app.UseRequestLocalization();
        app.UseStaticFiles();
        app.UseRouting();
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseEndpoints(endpoints =>
        {
            endpoints.MapControllers();
            endpoints.MapXafEndpoints();
        });
    }
}
