using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace NimbleNet
{
    public partial class NimbleAgent
    {
        private Socket? _udpSocketv4;
        private Socket? _udpSocketv6;

        public static readonly bool IPv6Support;
        public bool IPv6Enabled;

        /// <summary>
        /// [Server Mode]
        /// Start logic thread and listening on selected port
        /// </summary>
        /// <param name="addressIPv4">bind to specific ipv4 address</param>
        /// <param name="addressIPv6">bind to specific ipv6 address</param>
        /// <param name="port">port to listen</param>
        public bool Start(IPAddress addressIPv4, IPAddress addressIPv6, int port)
        {
            if (IsRunning && IsConnected)
                return false;

            IsConnected = true;
            // IPv4 soketini oluştur
            _udpSocketv4 = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

            // IPv4 soketi başarıyla bağlanabiliyor mu?
            if (!BindSocket(_udpSocketv4, new IPEndPoint(addressIPv4, port)))
                return false;

            var LocalPort = ((IPEndPoint)_udpSocketv4.LocalEndPoint).Port;
            Console.WriteLine($"Bound to IPv4 address: {addressIPv4}:{LocalPort}");

            IsRunning = true;

            // IPv6 desteği varsa, IPv6 soketini oluştur
            if (IPv6Support && IPv6Enabled)
            {
                _udpSocketv6 = new Socket(AddressFamily.InterNetworkV6, SocketType.Dgram, ProtocolType.Udp);

                // IPv6 soketini aynı port ile bağla
                if (!BindSocket(_udpSocketv6, new IPEndPoint(addressIPv6, LocalPort)))
                {
                    Console.WriteLine("Failed to bind IPv6 socket.");
                    _udpSocketv6 = null;
                }
                else
                {
                    Console.WriteLine($"Bound to IPv6 address: {addressIPv6}:{LocalPort}");
                }
            }

            // Dinleyici thread'i başlat
            Thread listenerThread = new Thread(ListenerThread);
            listenerThread.Start();

            return true;
        }

        /// <summary>
        /// [Client Mode]
        /// Connect to a remote server as a client.
        /// </summary>
        /// <param name="serverAddress">Server IP address</param>
        /// <param name="port">Server port</param>
        public bool Connect(IPAddress serverAddress, int port)
        {
            if (IsRunning)
                return false;

            _udpSocketv4 = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

            try
            {
                _udpSocketv4.Connect(new IPEndPoint(serverAddress, port));
                IsRunning = true;

                Console.WriteLine($"Connected to server at {serverAddress}:{port}");

                // Dinleyici thread'ini başlat
                Thread listenerThread = new Thread(ListenerThread);
                listenerThread.Start();

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to connect: {ex.Message}");
                return false;
            }
        }

        private bool BindSocket(Socket socket, IPEndPoint target)
        {
            try
            {
                socket.Bind(target);
                Console.WriteLine($"Socket bound to {target.ToString()}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to bind socket to {target}: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Send data to the server.
        /// </summary>
        /// <param name="data">Data to send</param>
        public void Send(byte[] data)
        {
            if (_udpSocketv4 == null || !IsRunning)
            {
                Console.WriteLine("Socket is not initialized or running.");
                return;
            }

            try
            {
                _udpSocketv4.Send(data);
                Console.WriteLine($"Sent {data.Length} bytes to server.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to send data: {ex.Message}");
            }
        }

        private void ListenerThread()
        {
            EndPoint bufferEndPoint = new IPEndPoint(IPAddress.Any, 0);
            var selectReadList = new List<Socket> { _udpSocketv4 };
            byte[] buffer = new byte[4096]; // Buffer size for received data

            while (IsRunning)
            {
                try
                {
                    // Poll the socket and check for incoming data
                    if (_udpSocketv4.Available == 0 && !_udpSocketv4.Poll(1000, SelectMode.SelectRead))
                        continue;

                    // Receive the data from the socket
                    int bytesReceived = ReceiveFrom(_udpSocketv4, ref bufferEndPoint, buffer);

                    if (bytesReceived > 0)
                    {
                        // Process received data (can raise event or handle data here)
                        byte[] receivedData = new byte[bytesReceived];
                        Array.Copy(buffer, receivedData, bytesReceived);
                        OnDataReceived?.Invoke(receivedData);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error in ListenerThread: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Receive data from the provided socket.
        /// </summary>
        private int ReceiveFrom(Socket socket, ref EndPoint endPoint, byte[] buffer)
        {
            try
            {
                return socket.ReceiveFrom(buffer, ref endPoint);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in ReceiveFrom: {ex.Message}");
                return 0;
            }
        }

        static NimbleAgent()
        {
#if DISABLE_IPV6
            IPv6Support = false;
#elif !UNITY_2019_1_OR_NEWER && !UNITY_2018_4_OR_NEWER && (!UNITY_EDITOR && ENABLE_IL2CPP)
            string version = UnityEngine.Application.unityVersion;
            IPv6Support = Socket.OSSupportsIPv6 && int.Parse(version.Remove(version.IndexOf('f')).Split('.')[2]) >= 6;
#else
            IPv6Support = Socket.OSSupportsIPv6;
#endif
        }
    }
}
