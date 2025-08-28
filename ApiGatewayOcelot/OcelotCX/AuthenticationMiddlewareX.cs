using Microsoft.AspNetCore.Authentication;
using Ocelot.Configuration;
using Ocelot.Logging;
using Ocelot.Middleware;

namespace ApiGatewayOcelot;

public sealed class AuthenticationMiddlewareX : OcelotMiddleware
{
    private readonly RequestDelegate _next;

    public AuthenticationMiddlewareX(RequestDelegate next, IOcelotLoggerFactory loggerFactory)
        : base(loggerFactory.CreateLogger<AuthenticationMiddlewareX>())
    {
        _next = next;
    }

    public async Task Invoke(HttpContext httpContext)
    {
        HttpRequest request = httpContext.Request;
        PathString path = httpContext.Request.Path;
        DownstreamRoute downstreamRoute = httpContext.Items.DownstreamRoute();
        if (request.Method.Equals("OPTIONS", StringComparison.OrdinalIgnoreCase) || !downstreamRoute.IsAuthenticated)
        {
            base.Logger.LogInformation($"No authentication needed for path '{path}'.");
            await _next(httpContext);
            return;
        }

        base.Logger.LogInformation(() => $"The path '{path}' is an authenticated route! {base.MiddlewareName} checking if client is authenticated...");

        if (httpContext.Request.Path.ToString().Contains("/Login/LoginUserFun"))
        {
            base.Logger.LogInformation($"'{path}'：个别接口跳过验证");
            await _next(httpContext);
            return;
        }

        Microsoft.Extensions.Primitives.StringValues selftoken = "";
        httpContext.Request.Headers.TryGetValue("token", out selftoken);
        if (!string.IsNullOrEmpty(selftoken.ToString()))
        {
            base.Logger.LogInformation($"'{path}'：Token跳过验证");
            await _next(httpContext);
            return;
        }

        AuthenticateResult authenticateResult = await AuthenticateAsync(httpContext, downstreamRoute);
        if (authenticateResult.Principal?.Identity == null)
        {
            SetUnauthenticatedError(httpContext, path, null);
            return;
        }

        httpContext.User = authenticateResult.Principal;
        if (httpContext.User.Identity.IsAuthenticated)
        {
            base.Logger.LogInformation(() => $"Client has been authenticated for path '{path}' by '{httpContext.User.Identity.AuthenticationType}' scheme.");
            await _next(httpContext);
        }
        else
        {
            SetUnauthenticatedError(httpContext, path, httpContext.User.Identity.Name);
        }
    }

    private void SetUnauthenticatedError(HttpContext httpContext, string path, string userName)
    {
        UnauthenticatedError error = new UnauthenticatedError("Request for authenticated route '" + path + "' " + (string.IsNullOrEmpty(userName) ? "was unauthenticated" : ("by '" + userName + "' was unauthenticated!")));
        base.Logger.LogWarning(() => $"Client has NOT been authenticated for path '{path}' and pipeline error set. {error};");
        httpContext.Items.SetError(error);
    }

    private async Task<AuthenticateResult> AuthenticateAsync(HttpContext context, DownstreamRoute route)
    {
        Ocelot.Configuration.AuthenticationOptions authenticationOptions = route.AuthenticationOptions;
        if (!string.IsNullOrWhiteSpace(authenticationOptions.AuthenticationProviderKey))
        {
            return await context.AuthenticateAsync(authenticationOptions.AuthenticationProviderKey);
        }

        string[] authenticationProviderKeys = authenticationOptions.AuthenticationProviderKeys;
        if (authenticationProviderKeys.Length == 0 || authenticationProviderKeys.All(string.IsNullOrWhiteSpace))
        {
            base.Logger.LogWarning(() => $"Impossible to authenticate client for path '{route.DownstreamPathTemplate}': both {"AuthenticationProviderKey"} and {"AuthenticationProviderKeys"} are empty but the {"AuthenticationOptions"} have defined.");
            return AuthenticateResult.NoResult();
        }

        AuthenticateResult result = null;
        foreach (string scheme in authenticationProviderKeys.Where((string apk) => !string.IsNullOrWhiteSpace(apk)))
        {
            try
            {
                result = await context.AuthenticateAsync(scheme);
                if (result?.Succeeded ?? false)
                {
                    return result;
                }
            }
            catch (Exception ex)
            {
                Exception e = ex;
                base.Logger.LogWarning(() => $"Impossible to authenticate client for path '{route.DownstreamPathTemplate}' and {"AuthenticationProviderKey"}:{scheme}. Error: {e.Message}.");
            }
        }

        return result ?? AuthenticateResult.NoResult();
    }
}