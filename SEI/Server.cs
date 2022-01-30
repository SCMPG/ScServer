using Comms;
using Engine;
using Game;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace SCMPG
{
    //@
    public class Server
    {
        private string ServerName;
        public Peer Peer;

        public bool IsDedicatedServer
        {
            get;
        }

        public bool IsUsingInProcessTransmitter
        {
            get;
        }

        public bool IsPaused
        {
            get;
            set;
        }

        public bool IsDisposing
        {
            get;
            private set;
        }

        public Server(bool useInProcessTransmitter)
        {
            Log.Information($"Please Input Server Name");
            ServerName = Console.ReadLine();
            Log.Information($"Starting Survivalcraft[{ServerName}] server on {DateTime.Now.ToUniversalTime():dd/MM/yyyy HH:mm:ss} UTC, version {Assembly.GetExecutingAssembly().GetName().Version}");
            IsUsingInProcessTransmitter = useInProcessTransmitter;
            IPacketTransmitter packetTransmitter = (!useInProcessTransmitter) ? new DiagnosticPacketTransmitter(new UdpPacketTransmitter(40102)) : new DiagnosticPacketTransmitter(new InProcessPacketTransmitter());
            Log.Information($"Server address is {packetTransmitter.Address}");
            Peer = new Peer(packetTransmitter);
            Peer.Settings.SendPeerConnectDisconnectNotifications = false;
            Peer.Comm.Settings.ResendPeriods = new float[5]
            {
                0.5f,
                0.5f,
                1f,
                1.5f,
                2f
            };
            Peer.Comm.Settings.MaxResends = 20;
            Peer.Settings.KeepAlivePeriod = (IsUsingInProcessTransmitter ? float.PositiveInfinity : 5f);
            Peer.Settings.KeepAliveResendPeriod = (IsUsingInProcessTransmitter ? float.PositiveInfinity : 2f);
            Peer.Settings.ConnectionLostPeriod = (IsUsingInProcessTransmitter ? float.PositiveInfinity : 30f);
            Peer.Settings.ConnectTimeOut = 6f;
            Peer.Error += delegate (Exception e)
            {
                Log.Error(e);
            };
            Peer.PeerDiscoveryRequest += delegate (Packet p)
            {
                if (!IsDisposing)
                {
                    HandlePeerDiscovery(p.Address);
                }
            };
            Peer.ConnectRequest += delegate (PeerPacket p)
            {
                if (!IsDisposing)
                {
                    switch (Message.Read(p.Data))
                    {
                        case GameListMessage:

                            break;
                    }
                }
            };
            Peer.PeerDisconnected += delegate (PeerData peerData)
            {
                if (!IsDisposing)
                {
                    /*HandleDisconnect(peerData);*/
                }
            };
            Peer.DataMessageReceived += delegate (PeerPacket p)
            {
                if (!IsDisposing)
                {
                    Message message = Message.Read(p.Data);

                }
            };
        }



        private void HandlePeerDiscovery(IPEndPoint address)
        {
            double realTime = Time.RealTime;
            var a = new GameListMessage {ServerName= ServerName ,ServerPriority= 100 };
            a.WorldInfo[0] = new Object.SWorldInfo {SerializationVersion="Obj",Size=0,LastSaveTime=DateTime.Now,DirectoryName="ss" };
            Peer.RespondToDiscovery(address, DeliveryMode.Unreliable, Message.Write(a));
            Log.Information("HandlePeerDiscoveryDone");
        }
    } 
}
    /*private void Handle(CreateGameMessage message, PeerData peerData)
    {
        if (VerifyPlayerName(message.CreationParameters.CreatingPlayerName))
        {
            RecentlySeenUsers[peerData.Address] = Time.RealTime;
            if (Config.ShutdownSequence)
            {
                Peer.RefuseConnect(peerData, Message.Write(new RefusedMessage
                {
                    Reason = "Server restarting, please wait a while and try again."
                }));
            }
            else if (Games.Count < 400)
            {
                ServerGame serverGame = new ServerGame(this, message, peerData);
                Games.Add(serverGame);
                Peer.AcceptConnect(peerData, Message.Write(new GameCreatedMessage
                {
                    GameId = serverGame.GameId,
                    CreationParameters = message.CreationParameters
                }));
                Log.Information($"Player {message.CreationParameters.CreatingPlayerName} at {peerData.Address} ({message.CreationParameters.CreatingPlayerPlatform}, {message.ReceivedVersion}) created game {serverGame.GameId} ({GetStatsString()})");
            }
            else
            {
                Peer.RefuseConnect(peerData, Message.Write(new RefusedMessage
                {
                    Reason = "Too many games in progress, please wait a while and try again."
                }));
            }
        }
        else
        {
            Peer.RefuseConnect(peerData, Message.Write(new RefusedMessage
            {
                Reason = "Please change your nickname in Settings."
            }));
        }
    }

    private void Handle(JoinGameMessage message, PeerData peerData)
    {
        if (VerifyPlayerName(message.PlayerName))
        {
            RecentlySeenUsers[peerData.Address] = Time.RealTime;
            ServerGame serverGame = Games.FirstOrDefault((ServerGame g) => g.GameId == message.GameId);
            if (serverGame != null)
            {
                serverGame.Handle(message, peerData);
                return;
            }

            Peer.RefuseConnect(peerData, Message.Write(new RefusedMessage
            {
                Reason = "Game does not exist"
            }));
        }
        else
        {
            Peer.RefuseConnect(peerData, Message.Write(new RefusedMessage
            {
                Reason = "Please change your nickname in Settings."
            }));
        }
    }

    private void Handle(StartGameMessage message, PeerData peerData)
    {
        RecentlySeenUsers[peerData.Address] = Time.RealTime;
        ServerHumanPlayer player = GetPlayer(peerData);
        if (player != null)
        {
            player.Game.Handle(message, player);
            return;
        }

        throw new InvalidOperationException($"Received StartGameMessage from unknown player at {peerData.Address}.");
    }

    private void Handle(PlayerOrdersMessage message, PeerData peerData)
    {
        RecentlySeenUsers[peerData.Address] = Time.RealTime;
        ServerHumanPlayer player = GetPlayer(peerData);
        if (player != null)
        {
            player.Game.Handle(message, player);
            return;
        }

        throw new InvalidOperationException($"Received PlayerOrdersMessage from unknown player at {peerData.Address}.");
    }

    private void Handle(GameImageMessage message, PeerData peerData)
    {
        RecentlySeenUsers[peerData.Address] = Time.RealTime;
        ServerHumanPlayer player = GetPlayer(peerData);
        if (player != null)
        {
            player.Game.Handle(message, player);
            return;
        }

        throw new InvalidOperationException($"Received GameImageMessage from unknown player at {peerData.Address}.");
    }

    private void Handle(GameStateMessage message, PeerData peerData)
    {
        RecentlySeenUsers[peerData.Address] = Time.RealTime;
        ServerHumanPlayer player = GetPlayer(peerData);
        if (player != null)
        {
            player.Game.Handle(message, player);
            return;
        }

        throw new InvalidOperationException($"Received GameStateMessage from unknown player at {peerData.Address}.");
    }

    private void Handle(GameStateHashMessage message, PeerData peerData)
    {
        RecentlySeenUsers[peerData.Address] = Time.RealTime;
        ServerHumanPlayer player = GetPlayer(peerData);
        if (player != null)
        {
            player.Game.Handle(message, player);
            return;
        }

        throw new InvalidOperationException($"Received GameStateHashMessage from unknown player at {peerData.Address}.");
    }
    private void HandleDisconnect(PeerData peerData)
    {
        ServerHumanPlayer player = GetPlayer(peerData);
        if (player != null)
        {
            player.Game.HandleDisconnect(player);
            return;
        }

        throw new InvalidOperationException($"Received GameStateMessage from unknown player at {peerData.Address}.");
    }
*/
   