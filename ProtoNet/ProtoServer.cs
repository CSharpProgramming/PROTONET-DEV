﻿using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace ProtoNet
{
    public class ProtoServer
    {
        public event ProtoClient.EventHandler<ProtoClient, ProtoPacket> PacketReceived;
        public event ProtoClient.EventHandler<ProtoClient, EventArgs> ClientConnected;
        public event ProtoClient.EventHandler<ProtoClient, string> ClientDisconnected;
        public event ProtoClient.EventHandler<ProtoClient, double> ClientPingUpdated;

        private Socket socket;
        private SocketAsyncEventArgs socketAsyncArgs;
        private List<ProtoClient> connectedClients;
        public IReadOnlyList<ProtoClient> ConnectedClients => connectedClients.AsReadOnly();
       

        public ProtoServer() {
            socket = new Socket(SocketType.Stream, ProtocolType.Tcp);
            socketAsyncArgs = new SocketAsyncEventArgs();
            socketAsyncArgs.Completed += AcceptAsyncCallback;
            connectedClients = new List<ProtoClient>();
        }

        private void AcceptAsyncCallback(object sender, SocketAsyncEventArgs e) {
            if (e.AcceptSocket != null) {
                ProtoClient ps = new ProtoClient(e.AcceptSocket);
                ps.Disconnected += Client_Disconnected;
                ps.PacketReceived += PacketReceived;
                ps.PingUpdated += Client_PingUpdated;
                Client_Connected(ps, EventArgs.Empty);
                ps.Start();
                e.AcceptSocket = null;
            }
            AcceptAsync();
        }

        private void Client_PingUpdated(ProtoClient sender, double eventArgs) {
            ClientPingUpdated?.Invoke(sender, eventArgs);
        }

        public void Listen(int port, int backLog) {
            socket.Bind(new IPEndPoint(IPAddress.Any, port));
            socket.Listen(backLog);

            AcceptAsync();
        }

        public void Broadcast(byte[] packet, ProtoClient exception = null) {
            for (int i = 0; i < connectedClients.Count; i++) {
                try {
                    if(connectedClients[i] != exception) {
                        connectedClients[i].Send(packet);
                    }
                } catch { }
            }
        }

        private void AcceptAsync() {
            socket.AcceptAsync(socketAsyncArgs);
        }

        private void Client_Connected(ProtoClient sender, EventArgs e) {
            AddClient(sender);
            ClientConnected?.Invoke(sender, EventArgs.Empty);
        }

        private void Client_Disconnected(ProtoClient sender, string e) {
            RemoveClient(sender);
            ClientDisconnected?.Invoke(sender, e);
        }

        public void AddClient(ProtoClient socket) {
            lock (connectedClients) {
                connectedClients.Add(socket);
            }
        }

        public void RemoveClient(ProtoClient socket) {
            lock (connectedClients) {
                connectedClients.Remove(socket);
            }
        }
    }
}
