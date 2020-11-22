using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ServerWebApplication;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

namespace Mstsc.Target
{
    public class Startup
    {
        //连接本地3389
        SocketConnect local3389 = new SocketConnect();

        //连接中心服务器
        SocketConnect center = new SocketConnect();
       


        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            new Task(async () =>
            {
                //连接本地3389
                await local3389.ConnectAsync("127.0.0.1", 3389);
                await center.ConnectAsync("zwovo.xyz", 3390);


                new Task(async () =>
                {
                    while (true)
                    {
                        //收到本地中心服务器数据
                        var readResult = await center.PipeReader.ReadAsync();
                        if (readResult.Buffer.IsEmpty)
                        {
                            break;
                        }

                        SequencePosition position = readResult.Buffer.Start;
                        if (readResult.Buffer.TryGet(ref position, out var memory))
                        {
                            //发送到local3389
                            await center.SendAsync(memory);

                            local3389.PipeReader.AdvanceTo(readResult.Buffer.GetPosition(memory.Length));
                        }

                        if (readResult.IsCanceled || readResult.IsCompleted)
                        {
                            break;
                        }
                    }

                    await local3389.PipeReader.CompleteAsync();
                }).Start();



                while (true)
                {
                    //收到本地3389数据
                    var readResult = await local3389.PipeReader.ReadAsync();
                    if (readResult.Buffer.IsEmpty)
                    {
                        break;
                    }

                    SequencePosition position = readResult.Buffer.Start;
                    if (readResult.Buffer.TryGet(ref position, out var memory))
                    {
                        //发送到中心服务器
                        await center.SendAsync(memory);

                        local3389.PipeReader.AdvanceTo(readResult.Buffer.GetPosition(memory.Length));
                    }

                    if (readResult.IsCanceled || readResult.IsCompleted)
                    {
                        break;
                    }
                }

                await local3389.PipeReader.CompleteAsync();
            }).Start();
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
