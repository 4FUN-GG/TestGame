using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.Threading.Tasks;
using EmbedIO.WebSockets;
using FourFun.Helpers;
using FourFun.Server;
using Newtonsoft.Json;
using NUnit.Framework;

public class WebSocketGameModule : WebSocketModule
{
    public static WebSocketGameModule Instance { get; private set; }

    public Dictionary<int, IWebSocketContext> clients = new Dictionary<int, IWebSocketContext>();

    public WebSocketGameModule(string urlPath) : base (urlPath, true)
    {
        Instance = this;
    }

    protected override Task OnClientConnectedAsync(IWebSocketContext context)
    {
        var requestParameters = LocalWebServer.ParseUrlParameters(context.RequestUri.ToString());
        if (!requestParameters.ContainsKey("playerId"))
        {
            // Do not allow connections without playerId
            CloseAsync(context);
            return Task.CompletedTask;
        }
        var playerId = int.Parse(requestParameters["playerId"]);
        UnityEngine.Debug.Log($"{playerId} connected");
        clients[playerId] = context;
        UnityMainThreadDispatcher.Enqueue(delegate
        {
            UnoGame.Instance.SetPlayerConnected(playerId);
        });
        return Task.CompletedTask;
    }

    public void Send(int playerId, string header, string message)
    {
        dynamic request = new ExpandoObject();
        request.name = header;
        request.message = message;

        string jsonString = JsonConvert.SerializeObject(request);

        SendAsync(clients[playerId], jsonString);
    }

    protected override Task OnMessageReceivedAsync(
        IWebSocketContext context,
        byte[] rxBuffer,
        IWebSocketReceiveResult rxResult)
    {
        var message = Encoding.GetString(rxBuffer);
        return Task.CompletedTask;
    }
}