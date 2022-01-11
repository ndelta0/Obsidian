//     Obsidian/RconServer.cs
//     Copyright (C) 2022

using Microsoft.Extensions.Logging;
using System.Buffers.Binary;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Obsidian.Net.Rcon;

public class RconServer
{
    private readonly ILogger logger;
    private readonly TcpListener listener;
    private readonly List<RconConnection> connections = new();

    private uint connectionId;

    public static string Password { get; private set; } = string.Empty;

    public RconServer(ILogger logger, ushort port, string password)
    {
        this.logger = logger;
        Password = password;
        listener = TcpListener.Create(port);
    }

    public async Task RunAsync(CancellationToken token)
    {
        _ = Task.Run(async () =>
        {
            while (!token.IsCancellationRequested)
                try
                {
                    connections.RemoveAll(c => !c.Connected);
                    await Task.Delay(5000, token);
                }
                catch (Exception e) when (e is TaskCanceledException or OperationCanceledException)
                {
                    return;
                }
        }, token);
        
        connectionId = 0;
        listener.Start();
        logger.LogInformation("Started RCON server");

        while (!token.IsCancellationRequested)
            try
            {
                var conn = await listener.AcceptTcpClientAsync(token);
                logger.LogInformation("Accepting RCON connection ID {ConnectionId} from {RemoteAddress}", ++connectionId, conn.Client.RemoteEndPoint as IPEndPoint);
                connections.Add(new RconConnection(connectionId, conn, logger, token));
            }
            catch (Exception e) when (e is TaskCanceledException or OperationCanceledException)
            {
                break;
            }

        logger.LogInformation("Stopping RCON server");
        listener.Stop();
    }
}
