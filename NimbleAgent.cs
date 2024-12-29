using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace NimbleNet
{
    public partial class NimbleAgent
    {
        
        private Thread nimbleThread;
        private bool isRunning;

        //Events
        public event Action<byte[]> OnDataReceived;
        public event Action<byte[]> OnAgentConnect;
        public event Action<byte[]> OnAgentDisconnect;

        public bool IsRunning;
        public bool IsConnected;
        public bool UseNativeSockets = false;

        public int LocalPort { get; private set; }



        // Start as networking entity
        //public void Start(string mode, string serverIp = null, int udpPort = 0)
        //{
        //    if (mode == "server")
        //    {
        //       // udpSocket.StartServer(udpPort);
        //        Console.WriteLine($"UDP Server started on port {udpPort}");
        //    }
        //    else if (mode == "client")
        //    {
        //        if (serverIp == null)
        //            throw new ArgumentException("Server IP is required for client mode.");

        //       // udpSocket.StartClient(serverIp, udpPort);
        //        Console.WriteLine($"UDP Client started and connected to {serverIp}:{udpPort}");
        //    }
        //    else
        //    {
        //        throw new ArgumentException("Invalid mode. Use 'server' or 'client'.");
        //    }

        //    // UDP dinleme thread'ini başlat
        //    isRunning = true;
        //   // nimbleThread = new Thread(new ThreadStart(UdpListenerThread));
        //    nimbleThread.Start();
        //}

    }
}
