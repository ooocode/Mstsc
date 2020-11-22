using Microsoft.AspNetCore.SignalR;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Mstsc.Server
{
    public class ChatHub : Hub
    {
        static System.Collections.Concurrent.ConcurrentDictionary<string, string> connects = new ConcurrentDictionary<string, string>();
        public ChatHub()
        {

        }


        public override async Task OnConnectedAsync()
        {
            Console.WriteLine("客户端连接:" + this.Context.ConnectionId);
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception exception)
        {
            var item = connects.FirstOrDefault(e => e.Key == this.Context.ConnectionId);
            connects.TryRemove(item);


            Console.WriteLine("客户端离线：" + this.Context.ConnectionId + " userId:" + item.Value);
            await base.OnDisconnectedAsync(exception);
        }

        public async Task Regist(string user)
        {
            connects.TryAdd(this.Context.ConnectionId, user);
        }


        public async Task SendMessage(string user, byte[] memory)
        {
            Console.WriteLine(user);
            Console.WriteLine(memory.Length);

            var connectIds = connects.Where(e => e.Value == user).Select(e => e.Key);
            Console.WriteLine("发送到：" + string.Join('、', connectIds) + " user=" + user);
            await Clients.Clients(connectIds).SendAsync("recv", memory);
            //await Clients.Group(groupName).SendAsync("recv", memory);
        }
    }
}
