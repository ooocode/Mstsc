using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ServerWebApplication;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace Mstsc.Client
{
    public class Startup
    {



        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            var factory = services.BuildServiceProvider().GetService<IConnectionListenerFactory>();
            Task.Run(async () =>
            {
                var listener = await factory.BindAsync(new IPEndPoint(IPAddress.Any, 1083));
                while (true)
                {
                    ConnectionContext client = await listener.AcceptAsync();
                    new Task(async () =>
                    {
                        await HandlerClientAsync(client);
                    }).Start();
                }
            });
        }


        public async Task HandlerClientAsync(ConnectionContext client)
        {
            HubConnection connection = new HubConnectionBuilder()
            .WithUrl(new Uri("http://zwovo.xyz:5000/chathub"))
            .Build();


            connection.On<byte[]>("recv", async (msg) =>
            {
                await client.Transport.Output.WriteAsync(msg);
            });

            await connection.StartAsync();

            try
            {
                //注册
                await connection.InvokeAsync("Regist", "1");

                while (true)
                {
                    //接受
                    var readResult = await client.Transport.Input.ReadAsync();
                    if (readResult.Buffer.IsEmpty)
                    {
                        break;
                    }

                    SequencePosition position = readResult.Buffer.Start;
                    if (readResult.Buffer.TryGet(ref position, out ReadOnlyMemory<byte> memory))
                    {
                        //发送到中心
                        await connection.InvokeAsync("SendMessage", "2", memory.ToArray());

                        client.Transport.Input.AdvanceTo(readResult.Buffer.GetPosition(memory.Length));
                    }

                    if (readResult.IsCompleted || readResult.IsCanceled)
                    {
                        break;
                    }
                }
            }
            catch(Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            finally
            {
                await connection.StopAsync();
                await client.Transport.Input.CompleteAsync();
            }
        }



        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapGet("/", async context =>
                {
                    await context.Response.WriteAsync("Hello World!");
                });
            });
        }
    }
}
