using System;
using System.Collections.Generic;
using System.IO.Pipelines;
using System.Linq;
using System.Net.Sockets;
using System.Threading.Tasks;

namespace ServerWebApplication
{
    public class SocketConnect
    {
        private Pipe Pipe;

        public PipeReader PipeReader => Pipe.Reader;

        private Socket socket;

        public SocketConnect()
        {
            Pipe = new Pipe();
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            socket.NoDelay = false;
        }

        public async Task ConnectAsync(string host, int port)
        {
            try
            {
                await socket.ConnectAsync(host, port);
                if (socket.Connected)
                {
                    new Task(async () => await this.RecvAsync()).Start();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                socket.Close();
            }
        }


        public async Task SendAsync(ReadOnlyMemory<byte> memory)
        {
            if (socket != null && socket.Connected)
            {
                await socket.SendAsync(memory, SocketFlags.None);
            }
        }

        private async Task RecvAsync()
        {
            var memeory = Pipe.Writer.GetMemory(8096);
            while (socket.Connected)
            {
                try
                {
                    var lenth = await socket.ReceiveAsync(memeory, SocketFlags.None);
                    if (lenth == 0)
                    {
                        break;
                    }

                    //写入管道
                    await Pipe.Writer.WriteAsync(memeory.Slice(0, lenth));
                }
                catch(Exception ex)
                {
                    Console.WriteLine(ex.Message);
                    break;
                }
            }
            socket.Close();
            await Pipe.Writer.CompleteAsync();
        }
    }
}
