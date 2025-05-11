using System;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Server.DbModel;
using Shared;
using Shared.Net;

namespace Server {
    public class Startup(Registry<BlockConfigJson> blockConfigJsonRegistry) : IAsyncDisposable {
        private Func<UniTask>? _stopServer;

        public void ConfigureServices(IServiceCollection services) {
            services.AddAuthorization();
            services.AddMemoryCache();
            services.AddIdentity<IdentityUser, IdentityRole>()
                .AddEntityFrameworkStores<GameSavesContext>()
                .AddDefaultTokenProviders();
            services.AddDbContext<GameSavesContext>(GameSavesContext.ConfigureOptions);
            services.AddSingleton<ISocketManager>(new SocketServer());
            services.AddSingleton<VoxelsEngineServer>(sp => {
                var serviceScopeFactory = sp.GetRequiredService<IServiceScopeFactory>();
                var ss = sp.GetRequiredService<ISocketManager>();
                return new VoxelsEngineServer(
                    serviceScopeFactory,
                    ss,
                    blockConfigJsonRegistry
                );
            });
        }

        public async ValueTask DisposeAsync() {
            if (_stopServer != null) await _stopServer.Invoke();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env) {
            if (env.IsDevelopment()) {
                app.UseDeveloperExceptionPage();
            }

            app.UseWebSockets(new WebSocketOptions {
                KeepAliveInterval = TimeSpan.FromSeconds(20)
            });
            app.UseRouting();

            app.UseAuthorization();
            app.UseAuthentication();

            app.Use(async (context, next) => {
                if (context.Request.Path == "/ws") {
                    if (context.WebSockets.IsWebSocketRequest) {
                        var socketServer = context.RequestServices.GetRequiredService<ISocketManager>();
                        using var webSocket = await context.WebSockets.AcceptWebSocketAsync();
                        await socketServer.HandleClientAsync(webSocket);
                    } else {
                        context.Response.StatusCode = StatusCodes.Status400BadRequest;
                    }
                } else {
                    await next();
                }
            });


            app.UseEndpoints(endpoints => {
                endpoints.MapGet("/", async context => {
                    await context.Response.WriteAsync("test ");
                });
            });

            var voxelsEngineServer = app.ApplicationServices.GetRequiredService<VoxelsEngineServer>();
            _stopServer = voxelsEngineServer.StopAsync;
            voxelsEngineServer.StartAsync(9006).Forget();
        }
    }
}