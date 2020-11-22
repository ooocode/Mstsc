using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Connections;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace Mstsc.Server
{
    public class Startup
    {
        ConcurrentBag<ConnectionContext> clients = new ConcurrentBag<ConnectionContext>();

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            var factory = services.BuildServiceProvider().GetService<IConnectionListenerFactory>();
            Task.Run(async () =>
            {
                var listener = await factory.BindAsync(new IPEndPoint(IPAddress.Any, 3390));
                while (true)
                {
                    ConnectionContext client = await listener.AcceptAsync();
                    clients.Add(client);

                    new Task(async () =>
                    {
                        try
                        {
                            await HandlerClientAsync(client);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.Message);
                        }

                    }).Start();
                }
            });
        }

        public async Task HandlerClientAsync(ConnectionContext client)
        {
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
                    //发送给其他客户端
                    var others = clients.Where(e => e != client);
                    Console.WriteLine($"其他客户端：  {others.Count()}");

                    others.AsParallel().ForAll(async (other) =>
                    {
                        await other.Transport.Output.WriteAsync(memory);
                    });

                    client.Transport.Input.AdvanceTo(readResult.Buffer.GetPosition(memory.Length));
                }

                if (readResult.IsCompleted || readResult.IsCanceled)
                {
                    break;
                }
            }

            await client.Transport.Input.CompleteAsync();
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
