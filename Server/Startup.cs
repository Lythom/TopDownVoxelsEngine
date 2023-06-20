using System;
using System.Net;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using MessagePack;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Server.DbModel;
using Shared.Net;

namespace Server {
    public class Startup : IAsyncDisposable {
        private Func<UniTask>? _stopServer;

        public void ConfigureServices(IServiceCollection services) {
            services.AddAuthorization();
            services.AddMemoryCache();
            services.AddIdentity<IdentityUser, IdentityRole>()
                .AddEntityFrameworkStores<GameSavesContext>()
                .AddDefaultTokenProviders();
            services.AddDbContext<GameSavesContext>(GameSavesContext.ConfigureOptions);
            // Warm up StarTeamServer service during startup
            services.AddSingleton<VoxelsEngineServer>(sp => {
                var serviceScopeFactory = sp.GetRequiredService<IServiceScopeFactory>();
                return new VoxelsEngineServer(serviceScopeFactory, new SocketServer());
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


            app.UseRouting();
            app.UseWebSockets();

            app.UseAuthorization();
            app.UseAuthentication();

            app.Use(async (context, next) => {
                if (context.Request.Path == "/ws") {
                    if (context.WebSockets.IsWebSocketRequest) {
                        using (WebSocket webSocket = await context.WebSockets.AcceptWebSocketAsync()) {
                            try {
                                var buffer = MessagePackSerializer.Serialize(new ErrorNetworkMessage("test"));
                                await webSocket.SendAsync(buffer,
                                    WebSocketMessageType.Binary,
                                    WebSocketMessageFlags.EndOfMessage, CancellationToken.None);
                                Console.WriteLine("Sent bytes : " + buffer.Length);
                            } catch (Exception e) {
                                Console.WriteLine(e);
                                throw;
                            }

                            // await WebSocketHandler.InitWebSocketAsync(webSocket, context);
                            await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "testClose", CancellationToken.None);
                        }
                    } else {
                        context.Response.StatusCode = (int) HttpStatusCode.BadRequest;
                    }
                } else {
                    await next();
                }
            });

            app.UseEndpoints(endpoints => {
                endpoints.MapGet("/", async context => {
                    await context.Response.WriteAsync("test " + context.RequestServices.GetRequiredService<WebSocketMessagingQueue>().OpenSocketsCount);
                });
            });

            var voxelsEngineServer = app.ApplicationServices.GetRequiredService<VoxelsEngineServer>();
            _stopServer = voxelsEngineServer.StopAsync;
            voxelsEngineServer.StartAsync().Forget();
        }
    }
}