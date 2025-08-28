namespace ApiGatewayOcelot
{
    /// <summary>
    /// 注册ocelot中间件
    /// </summary>
    public static class RegisterOcelotExtension
    {
        public static IApplicationBuilder OcelotRegister<T>(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<T>();
        }
    }

}
