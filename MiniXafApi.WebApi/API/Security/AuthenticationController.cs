using DevExpress.ExpressApp;
using DevExpress.ExpressApp.Security;
using Microsoft.AspNetCore.Mvc;
using MiniXafApi.WebApi.API.Security;
using Swashbuckle.AspNetCore.Annotations;

namespace MiniXafApi.WebApi.JWT;

[ApiController]
[Route("api/[controller]")]
public class AuthenticationController : ControllerBase
{
    readonly JwtTokenProviderService defaultTokenProvider;
    readonly RopcTokenProviderService ropcTokenProvider;

    public AuthenticationController(
        JwtTokenProviderService defaultTokenProvider,
        RopcTokenProviderService ropcTokenProvider)
    {
        this.defaultTokenProvider = defaultTokenProvider;
        this.ropcTokenProvider = ropcTokenProvider;
    }

    [HttpPost("Authenticate")]
    [SwaggerOperation("Checks if the user with the specified logon parameters exists in the database. If it does, authenticates this user.", "Refer to the following help topic for more information on authentication methods in the XAF Security System: <a href='https://docs.devexpress.com/eXpressAppFramework/119064/data-security-and-safety/security-system/authentication'>Authentication</a>.")]
    public IActionResult Authenticate(
        [FromBody]
        [SwaggerRequestBody(@"For example: <br /> { ""userName"": ""Admin"", ""password"": """" }")]
        AuthenticationStandardLogonParameters logonParameters
    )
    {
        try
        {
            return Ok(defaultTokenProvider.Authenticate(logonParameters));
        }
        catch (AuthenticationException ex)
        {
            return Unauthorized(ex.GetJson());
        }
    }

    [HttpPost("RopcAuthenticate")]
    [SwaggerOperation("Checks if the user with the specified logon parameters exists in the database. If it does, authenticates this user.", "Refer to the following help topic for more information on authentication methods in the XAF Security System: <a href='https://docs.devexpress.com/eXpressAppFramework/119064/data-security-and-safety/security-system/authentication'>Authentication</a>.")]
    public async Task<IActionResult> RopcAuthenticate(
        [FromBody]
        [SwaggerRequestBody(@"For example: <br /> { ""userName"": ""Admin"", ""password"": """" }")]
        AuthenticationStandardLogonParameters logonParameters
    )
    {
        try
        {
            return Ok(await ropcTokenProvider.LoginAsync(logonParameters.UserName, logonParameters.Password));
        }
        catch (AuthenticationException ex)
        {
            return Unauthorized(ex.GetJson());
        }
    }
}
