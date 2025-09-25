using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Security;
using Microsoft.Extensions.Options;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MiniXafApi.WebApi.API.Security;

public class RopcTokenProviderService
{
    readonly HttpClient HttpClient;
    readonly IServiceProvider ServiceProvider;
    readonly RopcOptions Options;

    public RopcTokenProviderService(IServiceProvider serviceProvider, HttpClient httpClient, RopcOptions options)
    {
        ServiceProvider = serviceProvider;
        HttpClient = httpClient;
        Options = options;
    }

    public RopcTokenProviderService(IServiceProvider serviceProvider, HttpClient httpClient, IOptions<RopcOptions> options)
    {
        ServiceProvider = serviceProvider;
        HttpClient = httpClient;
        Options = options.Value;
    }

    public async Task<TokenResponse> LoginAsync(string username, string password)
    {
        var requestBody = new Dictionary<string, string>
        {
            { "grant_type", "password" },
            { "client_id", Options.ClientId },
            { "client_secret", Options.ClientSecret },
            { "username", username },
            { "password", password }
        };

        var content = new FormUrlEncodedContent(requestBody);
        var response = await HttpClient.PostAsync(Options.TokenEndpoint, content);
        if (response.IsSuccessStatusCode)
        {
            var responseContent = await response.Content.ReadAsStringAsync();
            var tokenResult = JsonSerializer.Deserialize<TokenResponse>(responseContent);

            //Generate ClaimPrincipal
            var token = new JwtSecurityTokenHandler().ReadJwtToken(tokenResult.AccessToken);
            var identity = new ClaimsPrincipal(new ClaimsIdentity(token.Claims, "Password"));

            IEnumerable<IAuthenticationProviderV2> services = ServiceProvider.GetServices<IAuthenticationProviderV2>();
            ISecurityStrategyBase requiredService2 = ServiceProvider.GetRequiredService<ISecurityStrategyBase>();
            INonSecuredObjectSpaceFactory requiredService3 = ServiceProvider.GetRequiredService<INonSecuredObjectSpaceFactory>();
            var authenticationProviderV = services.OfType<RopcAuthenticationProvider>().FirstOrDefault();
            if (authenticationProviderV == null)
            {
                throw new ArgumentException("There are no registered authentication providers that implement the " + typeof(RopcAuthenticationProvider).FullName + " interface");
            }

            using IObjectSpace objectSpace = requiredService3.CreateNonSecuredObjectSpace(requiredService2.UserType);

            authenticationProviderV.Authenticate(objectSpace, identity);

            return tokenResult!;
        }

        throw new Exception("failed");
    }
}

public class TokenResponse
{
    [JsonPropertyName("access_token")]
    public string AccessToken { get; set; }

    [JsonPropertyName("expires_in")]
    public int ExpiresIn { get; set; }

    [JsonPropertyName("refresh_token")]
    public string RefreshToken { get; set; }
}