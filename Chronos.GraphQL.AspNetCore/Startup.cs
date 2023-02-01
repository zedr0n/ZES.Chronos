using HotChocolate.AspNetCore;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using ZES.GraphQL;

#pragma warning disable CS0618

namespace Chronos.GraphQL.AspNetCore
{
    public class Startup
    {
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddCors();
            services.AddGraphQLServer();
            services.UseGraphQl(new[] { typeof(Chronos.Coins.Config), typeof(Chronos.Accounts.Config), typeof(Chronos.Hashflare.Config), typeof(Chronos.Core.Config) });
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseCors(builder =>
                builder
                    .AllowAnyOrigin()
                    .AllowAnyHeader()
                    .AllowAnyMethod());
            app.UseRouting()
                .UseEndpoints(x => x.MapGraphQL("/"));
        }
    }
}