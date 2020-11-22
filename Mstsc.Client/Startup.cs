using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
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
        SocketConnect local3389 = new SocketConnect();


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
                        try
                        {
                            await HandlerClientAsync(client);
                        }
                        catch(Exception ex)
                        {
                            Console.WriteLine(ex.Message);
                        }
                        
                    }).Start();
                }
            });

            //new Task(async () =>
            //{
            //    //连接本地3389
            //    await local3389.ConnectAsync("127.0.0.1", 3389);
            //}).Start();
        }


        public async Task HandlerClientAsync(ConnectionContext client)
        {
            SocketConnect target = new SocketConnect();
            await target.ConnectAsync("zwovo.xyz", 3390);

            new Task(async () =>
            {
                await HandlerTargetAsync(target, client);
            }).Start();

            while (true)
            {
                var readResult = await client.Transport.Input.ReadAsync();
                if (readResult.Buffer.IsEmpty)
                {
                    break;
                }

                SequencePosition position = readResult.Buffer.Start;
                if (readResult.Buffer.TryGet(ref position, out var memory))
                {
                    await target.SendAsync(memory);
                    client.Transport.Input.AdvanceTo(readResult.Buffer.GetPosition(memory.Length));
                }

                if (readResult.IsCompleted || readResult.IsCanceled)
                {
                    break;
                }
            }

            await client.Transport.Input.CompleteAsync();
        }

        public async Task HandlerTargetAsync(SocketConnect target, ConnectionContext client)
        {
            while (true)
            {
                var readResult = await target.PipeReader.ReadAsync();
                if (readResult.Buffer.IsEmpty)
                {
                    break;
                }

                SequencePosition position = readResult.Buffer.Start;
                if (readResult.Buffer.TryGet(ref position, out var memory))
                {
                    //发往客户端
                    await client.Transport.Output.WriteAsync(memory);
                    target.PipeReader.AdvanceTo(readResult.Buffer.GetPosition(memory.Length));
                }

                if (readResult.IsCanceled || readResult.IsCompleted)
                {
                    break;
                }
            }
            await target.PipeReader.CompleteAsync();
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
