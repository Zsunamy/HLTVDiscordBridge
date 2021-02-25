using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using Newtonsoft.Json.Linq;
using System.IO;

namespace HLTVDiscordBridge.Modules
{
    public class Scoreboard
    {
        public delegate Task MessageReceivedDelegate(string message);
        public event MessageReceivedDelegate OnKillfeedUpdate;

        ClientWebSocket _webSocket = new ClientWebSocket();
        byte[] buffer = new byte[100000000];
        uint _matchId;

        public Scoreboard(uint matchId)
        {
            _matchId = matchId;
        }

        public async Task ConnectWebSocket()
        { 
            await _webSocket.ConnectAsync(new Uri("ws://revilum.com:3000/api/scoreboard/2346455"), CancellationToken.None);
            await Receive();
        } 
        private async Task Receive()
        {            
            WebSocketReceiveResult res = await _webSocket.ReceiveAsync(buffer, CancellationToken.None);
            if (res.Count != 0 || res.CloseStatus == WebSocketCloseStatus.Empty)
            {
                string resultString = Encoding.UTF8.GetString(buffer);
                JObject result = JObject.Parse(resultString);
                JToken jTok;
                if (result.TryGetValue("log", out jTok))
                {
                    await OnKillfeedUpdate(jTok.ToString());
                }
                Array.Clear(buffer, 0, buffer.Length);
                await Receive();
            }
            Array.Clear(buffer, 0, buffer.Length);
        }

    }
}
