using IdentityServer;
using IdentityServer4.Configuration;
using Serilog;
using Serilog.Events;
using Serilog.Sinks.SystemConsole.Themes;

var builder = WebApplication.CreateBuilder(args);
string urls = AppSetting.GetConfig("Urls");
if (!string.IsNullOrEmpty(urls))
    builder.WebHost.UseUrls(urls);

//ע��MVC����
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

    // ��������Զ����û�����ѡ��         
    options.UserInteraction = new UserInteractionOptions
    {
        LoginUrl = "/AuthLogin/Index",//��¼��ַ  
        LogoutUrl = "/AuthLogin/Logout",//�˳���ַ 
        ConsentUrl = "/AuthLogin/Consent",//������Ȩͬ��ҳ���ַ
        ErrorUrl = "/Account/Error", //����ҳ���ַ
        LoginReturnUrlParameter = "ReturnUrl",//���ô��ݸ���¼ҳ��ķ���URL���������ơ�Ĭ��ΪreturnUrl 
        LogoutIdParameter = "logoutId", //���ô��ݸ�ע��ҳ���ע����ϢID���������ơ�ȱʡΪlogoutId 
        ConsentReturnUrlParameter = "ReturnUrl", //���ô��ݸ�ͬ��ҳ��ķ���URL���������ơ�Ĭ��ΪreturnUrl
        ErrorIdParameter = "errorId", //���ô��ݸ�����ҳ��Ĵ�����ϢID���������ơ�ȱʡΪerrorId
        CustomRedirectReturnUrlParameter = "ReturnUrl", //���ô���Ȩ�˵㴫�ݸ��Զ����ض���ķ���URL���������ơ�Ĭ��ΪreturnUrl                   
        CookieMessageThreshold = 5 //�����������Cookie�Ĵ�С�����ƣ�����Cookies���������ƣ���Ч�ı�֤��������򿪶��ѡ���һ��������Cookies���ƾͻ������ǰ��Cookiesֵ
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
               //ע���Զ����û���¼��֤
               .AddResourceOwnerValidator<ResourceOwnerPasswordValidator>()
               //�Զ��巵���û���Ϣ
               .AddProfileService<CustomProfileService>();

builder.Services.ConfigureNonBreakingSameSiteCookies();//�ƹ�https��֤

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

////����HTTPSת��
//app.UseHttpsRedirection();

app.UseCookiePolicy();

//���þ��ļ�
app.UseDefaultFiles();
app.UseStaticFiles();

//���CSP����
app.Use(async (context, next) =>
{
    context.Response.Headers["Content-Security-Policy"] = "script-src 'self' 'unsafe-inline' ";
    await next();
});

// ����·��
app.UseRouting();
app.UseCors();

//���IDS4�м����
//����������������µ�ַ���� IdentityServer4 �ķ����ĵ���https://localhost:6001/.well-known/openid-configuration
app.UseIdentityServer();
//ʹ����֤�м��
app.UseAuthorization();

//�ս��
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=AuthLogin}/{action=Index}/{id?}");

app.Run();
