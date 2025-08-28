using ApiGatewayOcelot;
using Microsoft.Net.Http.Headers;
using Ocelot.Authentication.Middleware;
using Ocelot.Authorization.Middleware;
using Ocelot.Cache.Middleware;
using Ocelot.Claims.Middleware;
using Ocelot.DownstreamPathManipulation.Middleware;
using Ocelot.DownstreamRouteFinder.Middleware;
using Ocelot.DownstreamUrlCreator.Middleware;
using Ocelot.Errors.Middleware;
using Ocelot.Headers.Middleware;
using Ocelot.LoadBalancer.Middleware;
using Ocelot.Middleware;
using Ocelot.Multiplexer;
using Ocelot.QueryStrings.Middleware;
using Ocelot.RateLimiting.Middleware;
using Ocelot.Request.Middleware;
using Ocelot.Requester.Middleware;
using Ocelot.RequestId.Middleware;
using Ocelot.Responder.Middleware;
using Ocelot.Security.Middleware;
using Ocelot.WebSockets;

public static class OcelotPipelineExtensionsX
{
    public static RequestDelegate BuildCustomeOcelotPipeline(this IApplicationBuilder app, OcelotPipelineConfiguration configuration)
    {
        app.UseMiddleware<ConfigurationMiddleware>();
        app.UseMiddleware<ExceptionHandlerMiddleware>();
        app.MapWhen(httpContext => httpContext.WebSockets.IsWebSocketRequest,
            ws =>
            {
                ws.UseMiddleware<DownstreamRouteFinderMiddleware>();
                ws.UseMiddleware<MultiplexingMiddleware>();
                ws.UseMiddleware<DownstreamRequestInitialiserMiddleware>();
                ws.UseMiddleware<LoadBalancingMiddleware>();
                ws.UseMiddleware<DownstreamUrlCreatorMiddleware>();
                ws.UseMiddleware<WebSocketsProxyMiddleware>();
            });
        app.UseIfNotNull(configuration.PreErrorResponderMiddleware);
        //app.UseMiddleware<ResponderMiddleware>();
        app.UseIfNotNull<ResponderMiddleware>(configuration.ResponderMiddleware);
        app.UseMiddleware<DownstreamRouteFinderMiddleware>();
        app.UseMiddleware<MultiplexingMiddleware>();
        app.UseMiddleware<SecurityMiddleware>();
        if (configuration.MapWhenOcelotPipeline != null)
        {
            //foreach (KeyValuePair<Func<HttpContext, bool>, Action<IApplicationBuilder>> item in configuration.MapWhenOcelotPipeline)
            //{
            //    app.MapWhen(item.Key, item.Value);
            //}
            foreach (var pipeline in configuration.MapWhenOcelotPipeline)
            {
                app.MapWhen(pipeline.Key, pipeline.Value);
            }
        }

        app.UseMiddleware<HttpHeadersTransformationMiddleware>();
        app.UseMiddleware<DownstreamRequestInitialiserMiddleware>();
        app.UseMiddleware<RateLimitingMiddleware>();
        app.UseMiddleware<RequestIdMiddleware>();
        app.UseIfNotNull(configuration.PreAuthenticationMiddleware);

        //重写
        if (configuration.AuthenticationMiddleware == null)
        {
            //自定义验证中间件
            app.OcelotRegister<AuthenticationMiddlewareX>();
        }
        else
        {
            //app.Use(configuration.AuthenticationMiddleware);
            app.UseIfNotNull<AuthenticationMiddleware>(configuration.AuthenticationMiddleware);
        }

        app.UseMiddleware<ClaimsToClaimsMiddleware>();

        app.UseIfNotNull(configuration.PreAuthorizationMiddleware);
        if (configuration.AuthorizationMiddleware == null)
        {
            app.UseMiddleware<AuthorizationMiddleware>();
        }
        else
        {
            //app.Use(configuration.AuthorizationMiddleware);
            app.UseIfNotNull<AuthorizationMiddleware>(configuration.AuthorizationMiddleware);
        }

        //app.UseMiddleware<ClaimsToHeadersMiddleware>();
        app.UseIfNotNull<ClaimsToHeadersMiddleware>(configuration.ClaimsToHeadersMiddleware);

        app.UseIfNotNull(configuration.PreQueryStringBuilderMiddleware);
        app.UseMiddleware<ClaimsToQueryStringMiddleware>();
        app.UseMiddleware<ClaimsToDownstreamPathMiddleware>();
        app.UseMiddleware<LoadBalancingMiddleware>();
        app.UseMiddleware<DownstreamUrlCreatorMiddleware>();
        //缓存中间件
        //app.UseMiddleware<OutputCacheMiddleware>();
        app.UseMiddleware<HttpRequesterMiddleware>();

        // 添加 CORS 头中间件
        app.Use(async (context, next) =>
        {
            if (!context.Response.Headers.ContainsKey(HeaderNames.AccessControlAllowOrigin))
            {
                var wildcard = new[] { "*" };
                context.Response.Headers.TryAdd(HeaderNames.AccessControlAllowOrigin, wildcard);
                context.Response.Headers.TryAdd(HeaderNames.AccessControlAllowHeaders, wildcard);
                context.Response.Headers.TryAdd(HeaderNames.AccessControlRequestMethod, wildcard);
            }
            await next();
        });
        //app.Use(async (context, next) =>
        //{
        //    if (!context.Response.Headers.ContainsKey(HeaderNames.AccessControlAllowOrigin))
        //    {
        //        context.Response.Headers.Add(HeaderNames.AccessControlAllowOrigin, new[] { "*" });
        //        context.Response.Headers.Add(HeaderNames.AccessControlAllowHeaders, new[] { "*" });
        //        context.Response.Headers.Add(HeaderNames.AccessControlRequestMethod, new[] { "*" });
        //    }
        //    await next();
        //});

        return app.Build();
    }

    //private static void UseIfNotNull(this IApplicationBuilder builder, Func<HttpContext, Func<Task>, Task> middleware)
    //{
    //    if (middleware != null)
    //    {
    //        builder.Use(middleware);
    //    }
    //}
    private static IApplicationBuilder UseIfNotNull(this IApplicationBuilder builder, Func<HttpContext, Func<Task>, Task> middleware)
    => middleware != null ? builder.Use(middleware) : builder;

    private static IApplicationBuilder UseIfNotNull<TMiddleware>(this IApplicationBuilder builder, Func<HttpContext, Func<Task>, Task> middleware)
        where TMiddleware : OcelotMiddleware => middleware != null
            ? builder.Use(middleware)
            : builder.UseMiddleware<TMiddleware>();
}