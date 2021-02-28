using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using Newtonsoft.Json.Linq;
using System.IO;
using Discord.WebSocket;
using Discord.Rest;
using Discord;

namespace HLTVDiscordBridge.Modules
{
    public class Scoreboard
    {
        public delegate Task MessageReceivedDelegate(string message, RestTextChannel channel);
        public event MessageReceivedDelegate OnKillfeedUpdate;

        ClientWebSocket _webSocket = new ClientWebSocket();
        byte[] buffer;
        uint _matchId;
        SocketGuild _guild;
        string _teams;
        int i = 0;

        public Scoreboard(uint matchId, SocketGuild guild, string teams)
        {
            buffer = new byte[10000000];
            _matchId = matchId;
            _guild = guild;
            _teams = teams;
            _ = ConnectWebSocket();
        }

        public async Task ConnectWebSocket()
        {
            OnKillfeedUpdate += KillFeedUpdate;
            bool createCategory = true;
            ulong categoryId = 0;
            RestTextChannel channel = null;
            //Channel erstellen
            foreach ( SocketCategoryChannel categoryChannel in _guild.CategoryChannels)
            {
                if (categoryChannel.Name == "scoreboard") { createCategory = false; categoryId = categoryChannel.Id; break; }
            } if(createCategory)
            {
                categoryId = (await _guild.CreateCategoryChannelAsync("scoreboard")).Id;
            }
            bool createChannel = true;
            foreach(SocketTextChannel can in _guild.TextChannels)
            {
                if (can.Name == _teams.ToLower().Replace(' ', '-').Replace(".", "")) { createChannel = false; }
            } if(createChannel)
            {
                channel = await _guild.CreateTextChannelAsync(_teams, x => { x.CategoryId = categoryId; });
            }

            await _webSocket.ConnectAsync(new Uri($"ws://revilum.com:3000/api/scoreboard/{_matchId}"), CancellationToken.None);
            await Receive(channel);
        }
        private async Task Receive(RestTextChannel channel)
        {
            Console.WriteLine("a");
            if(channel == null) { return; }
            WebSocketReceiveResult res = await _webSocket.ReceiveAsync(buffer, CancellationToken.None);
            if (res.Count != 0 || res.CloseStatus == WebSocketCloseStatus.Empty)
            {
                string resultString = Encoding.UTF8.GetString(buffer);
                JObject result = JObject.Parse(resultString);
                JToken jTok;
                if (result.TryGetValue("log", out jTok))
                {
                    await OnKillfeedUpdate(jTok.ToString(), channel);
                }
                Array.Clear(buffer, 0, buffer.Length);
                await Receive(channel);
                return;
            }
            Array.Clear(buffer, 0, buffer.Length);
            //await Receive(channel);
        }
        private async Task KillFeedUpdate(string msg, RestTextChannel channel)
        {
            if(i == 0) { File.WriteAllText("./cache/test.json", msg); i++; return; }
            await channel.SendMessageAsync(msg);
            i++;
        }
    }
}
