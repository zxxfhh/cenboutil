using ApiGatewayOcelot;
using IdentityServer4.AccessTokenValidation;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.IdentityModel.Logging;
using Ocelot.Cache.CacheManager;
using Ocelot.Configuration;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using Ocelot.Provider.Consul;
using Ocelot.Provider.Polly;
using Ocelot.ServiceDiscovery.Providers;
using Serilog;
using Serilog.Events;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

// 配置 Kestrel 的请求体大小
builder.WebHost.ConfigureKestrel(serverOptions =>
{
    serverOptions.Limits.MaxRequestBodySize = int.MaxValue;
});

#region 配置Serilog

string id4logDir = Path.Combine(AppContext.BaseDirectory, "id4logs");
if (!Directory.Exists(id4logDir)) Directory.CreateDirectory(id4logDir);
string outputTemplate = $"[时间:{{Timestamp:HH:mm:ss}} 等级:{{Level}}](来源:{{SourceContext}}){{NewLine}}消息:{{Message:lj}}{{NewLine}}{{Exception}}{{NewLine}}";

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()  //日志记录起始等级
    .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
    .MinimumLevel.Override("System", LogEventLevel.Information)
    .Enrich.FromLogContext()
    .WriteTo.File(
            path: $"{id4logDir}/log-.txt",
            fileSizeLimitBytes: 1024 * 1024 * 20, // 20MB
            rollOnFileSizeLimit: true,
            encoding: Encoding.UTF8,
            shared: true,
            outputTemplate: outputTemplate,
            retainedFileCountLimit: 31,
            rollingInterval: RollingInterval.Day,
            flushToDiskInterval: TimeSpan.FromSeconds(1))
    .CreateLogger();
builder.Host.UseSerilog();

#endregion

builder.Configuration.AddJsonFile("OcelotConfig.json", false, true);

string idsurl = AppSetting.GetConfig("IdentityServerConfig:Authority");
if (!string.IsNullOrEmpty(idsurl))
{
    var authenticationProviderKey = "ApiGatewayOcelot"; //这个为上面配置里的key
    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddIdentityServerAuthentication(authenticationProviderKey, options =>
            {
                options.Authority = idsurl;
                options.ApiName = AppSetting.GetConfig("IdentityServerConfig:Scope");
                options.ApiSecret = AppSetting.GetConfig("IdentityServerConfig:ClientSecrets");
                options.RequireHttpsMetadata = false; //不使用https
                options.SupportedTokens = SupportedTokens.Both;
            });
    Log.Information("Ids4加载成功");
}

IdentityModelEventSource.ShowPII = true;
IdentityModelEventSource.LogCompleteSecurityArtifact = true;

Func<IServiceProvider, DownstreamRoute, IServiceDiscoveryProvider, CustomLoadBalancer> loadBalancerFactoryFunc = (serviceProvider, Route, serviceDiscoveryProvider) => new CustomLoadBalancer(serviceDiscoveryProvider.GetAsync);

builder.Services
    .AddOcelot()
    .AddConsul()
    .AddPolly()
    //.AddCacheManager(config => config.WithDictionaryHandle())
    .AddCustomLoadBalancer(loadBalancerFactoryFunc);

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(builder =>
    {
        builder.SetIsOriginAllowed(_ => true)
               .AllowCredentials()
               .AllowAnyHeader()
               .AllowAnyMethod();
    });
});

//配置 IIS 的请求体大小
builder.Services.Configure<IISServerOptions>(options =>
{
    options.MaxRequestBodySize = int.MaxValue;
});
//文件上传大小配置
builder.Services.Configure<FormOptions>(options =>
{
    options.ValueLengthLimit = int.MaxValue;
    options.MultipartBodyLengthLimit = int.MaxValue;
    options.MultipartHeadersLengthLimit = int.MaxValue;
});

// Add services to the container.
var app = builder.Build();

////启用静态文件
//app.UseStaticFiles();

//app.UseHttpsRedirection();

//PreErrorResponderMiddleware - 上面已经解释过了.
//PreAuthenticationMiddleware - 这个允许用户执行预认证逻辑，然后再调用 Ocelot的认证中间件。IOcelotPreProcessor  
//AuthenticationMiddleware - 可以重写Ocelot的认证中间件。
//PreAuthorisationMiddleware - 这个允许用户执行预授权逻辑，然后再调用 Ocelot的授权中间件。
//AuthorisationMiddleware - 可以重写Ocelot的授权中间件。
//PreQueryStringBuilderMiddleware - 这允许用户在传递给Ocelot请求创建器之前在http请求上处理查询字符串。

//注册自定义日志中间件
app.UseMiddleware<OcelotLogWare>();

app.UseRouting();
app.UseCors();

//授权
app.UseAuthentication();
//使用认证中间件
app.UseAuthorization();

app.UseOcelot((build, config) =>
{
    build.BuildCustomeOcelotPipeline(config); //自定义ocelot中间件完成
}).Wait();

app.Run();
