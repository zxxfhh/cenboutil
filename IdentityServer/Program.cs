using IdentityServer;
using IdentityServer4.Configuration;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.SystemConsole.Themes;

var builder = WebApplication.CreateBuilder(args);
string urls = AppSetting.GetConfig("Urls");
if (!string.IsNullOrEmpty(urls))
    builder.WebHost.UseUrls(urls);

//注册MVC服务。
builder.Services.AddControllersWithViews();

string id4logDir = Path.Combine(AppContext.BaseDirectory, "id4log");
if (!Directory.Exists(id4logDir)) Directory.CreateDirectory(id4logDir);
builder.Host.UseSerilog((context, configuration) =>
{
    configuration
        //.MinimumLevel.Debug()
        .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
        .MinimumLevel.Override("System", LogEventLevel.Warning)
        //.MinimumLevel.Override("Microsoft.AspNetCore.Authentication", LogEventLevel.Information)
        .Enrich.FromLogContext()
        //.WriteTo.File($"{id4logDir}/identityserver4_log.txt")
        .WriteTo.File(
                path: $"{id4logDir}/log-.txt",
                fileSizeLimitBytes: 1024 * 1024 * 20, // 20MB
                rollOnFileSizeLimit: true,
                shared: true,
                rollingInterval: RollingInterval.Day,
                flushToDiskInterval: TimeSpan.FromSeconds(1))
        .WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level}] {SourceContext}{NewLine}{Message:lj}{NewLine}{Exception}{NewLine}", theme: AnsiConsoleTheme.Literate);
});

string CredentialDir = Path.Combine(AppContext.BaseDirectory, "Credential");
if (!Directory.Exists(CredentialDir)) Directory.CreateDirectory(CredentialDir);
string filename = Path.Combine(CredentialDir, "tempkey.jwk");
builder.Services.AddIdentityServer(options =>
{
    options.Events.RaiseErrorEvents = true;
    options.Events.RaiseInformationEvents = true;
    options.Events.RaiseFailureEvents = true;
    options.Events.RaiseSuccessEvents = true;

    //options.IssuerUri = "http://identity.cenbo.com";

    // 就是这里，自定义用户交互选项         
    options.UserInteraction = new UserInteractionOptions
    {
        LoginUrl = "/AuthLogin/Index",//登录地址  
        LogoutUrl = "/AuthLogin/Logout",//退出地址 
        ConsentUrl = "/AuthLogin/Consent",//允许授权同意页面地址
        ErrorUrl = "/Account/Error", //错误页面地址
        LoginReturnUrlParameter = "ReturnUrl",//设置传递给登录页面的返回URL参数的名称。默认为returnUrl 
        LogoutIdParameter = "logoutId", //设置传递给注销页面的注销消息ID参数的名称。缺省为logoutId 
        ConsentReturnUrlParameter = "ReturnUrl", //设置传递给同意页面的返回URL参数的名称。默认为returnUrl
        ErrorIdParameter = "errorId", //设置传递给错误页面的错误消息ID参数的名称。缺省为errorId
        CustomRedirectReturnUrlParameter = "ReturnUrl", //设置从授权端点传递给自定义重定向的返回URL参数的名称。默认为returnUrl                   
        CookieMessageThreshold = 5 //由于浏览器对Cookie的大小有限制，设置Cookies数量的限制，有效的保证了浏览器打开多个选项卡，一旦超出了Cookies限制就会清除以前的Cookies值
    };
})
               //.AddInMemoryIdentityResources(ServerConfig.GetIdentityResources())
               //.AddInMemoryApiResources(ServerConfig.GetApiResources())
               //.AddInMemoryApiScopes(ServerConfig.GetApiScopes())
               .AddResourceStore<DynamicResourcesStore>()
               //.AddClientStoreCache<DynamicClientStore>()
               .AddClientStore<DynamicClientStore>()
               //.AddInMemoryClients(ServerConfig.GetClients())
               .AddDeveloperSigningCredential(true, filename)
               //注入自定义用户登录验证
               .AddResourceOwnerValidator<ResourceOwnerPasswordValidator>()
               //自定义返回用户信息
               .AddProfileService<CustomProfileService>();

builder.Services.ConfigureNonBreakingSameSiteCookies();//绕过https认证

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(builder =>
    {
        builder.AllowAnyOrigin()
               .SetIsOriginAllowedToAllowWildcardSubdomains()
               .AllowAnyHeader()
               .AllowAnyMethod();
    });
});

var app = builder.Build();

////启用HTTPS转向
//app.UseHttpsRedirection();

app.UseCookiePolicy();

//启用静文件
app.UseDefaultFiles();
app.UseStaticFiles();

//解决CSP问题
app.Use(async (context, next) =>
{
    context.Response.Headers["Content-Security-Policy"] = "script-src 'self' 'unsafe-inline' ";
    await next();
});

// 启用路由
app.UseRouting();
app.UseCors();

//添加IDS4中间件。
//在浏览器中输入如下地址访问 IdentityServer4 的发现文档：https://localhost:6001/.well-known/openid-configuration
app.UseIdentityServer();
//使用认证中间件
app.UseAuthorization();

//终结点
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=AuthLogin}/{action=Index}/{id?}");

app.Run();
