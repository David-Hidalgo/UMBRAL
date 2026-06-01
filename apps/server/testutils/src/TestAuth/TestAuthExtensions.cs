using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection;

namespace TestUtils.TestAuth;

public static class TestAuthExtensions
{
    public static AuthenticationBuilder AddTestAuthentication(this IServiceCollection services, string scheme = "Test")
    {
        return services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = scheme;
            options.DefaultChallengeScheme = scheme;
        }).AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(scheme, _ => { });
    }
}
