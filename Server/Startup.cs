using System;
using System.Net;
using System.Net.WebSockets;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebSockets;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Server.DbModel;

namespace Server {
    public class Startup {
        public void ConfigureServices(IServiceCollection services) {
            services.AddMemoryCache();
            services.AddWebSockets(options => {
                options.KeepAliveInterval = TimeSpan.FromSeconds(15);
            });
            services.AddDbContext<GameSavesContext>(GameSavesContext.ConfigureOptions);
            services.AddSingleton<WebSocketMessagingQueue>();
            services.AddHostedService(p => p.GetRequiredService<WebSocketMessagingQueue>());
            // Warm up StarTeamServer service during startup
            var starTeamServer = new VoxelsEngineServer();
            services.AddSingleton(_ => starTeamServer);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env) {
            if (env.IsDevelopment()) {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();
            app.UseWebSockets();

            app.Use(async (context, next) => {
                if (context.Request.Path == "/ws") {
                    if (context.WebSockets.IsWebSocketRequest) {
                        using (WebSocket webSocket = await context.WebSockets.AcceptWebSocketAsync()) {
                            await WebSocketHandler.InitWebSocketAsync(webSocket, context);
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
        }
    }
}