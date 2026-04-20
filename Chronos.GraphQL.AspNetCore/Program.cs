using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using ZES.GraphQL;

#pragma warning disable ASPDEPR008

namespace Chronos.GraphQL.AspNetCore
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateWebHostBuilder(args).Build().Run();
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            WebHost.CreateDefaultBuilder(args)
                .UseStartup<Startup>()
                .UseKestrel(options =>
                {
                    options.ConfigureEndpointDefaults(o => o.Protocols = HttpProtocols.Http1AndHttp2);
                    options.ConfigureEndpoints();
                })
                .UseUrls("http://localhost:5000", "https://localhost:5001");
    }
}