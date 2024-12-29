using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Net;
using System.Threading;

namespace NimbleNet
{
    public partial class NimbleAgent
    {
        private Socket? _udpSocketv4;
        private Socket? _udpSocketv6;

        public static readonly bool IPv6Support;
        public bool IPv6Enabled;



        /// <summary>
        /// Start logic thread and listening on selected port
        /// </summary>
        public bool Start(IPAddress addressIPv4, IPAddress addressIPv6, int port, bool isHost, bool connectToSelf = false, IPAddress? serverAddress = null)
        {
            if (IsRunning)
                return false;

            if (isHost)
            {
                Console.WriteLine("Starting as host...");

                IsConnected = true;
                _udpSocketv4 = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

                if (!BindSocket(_udpSocketv4, new IPEndPoint(addressIPv4, port)))
                    return false;

                var localPort = ((IPEndPoint)_udpSocketv4.LocalEndPoint).Port;
                Console.WriteLine($"Bound to IPv4 address: {addressIPv4}:{localPort}");

                IsRunning = true;

                if (IPv6Support && IPv6Enabled)
                {
                    _udpSocketv6 = new Socket(AddressFamily.InterNetworkV6, SocketType.Dgram, ProtocolType.Udp);

                    if (!BindSocket(_udpSocketv6, new IPEndPoint(addressIPv6, localPort)))
                    {
                        Console.WriteLine("Failed to bind IPv6 socket.");
                        _udpSocketv6 = null;
                    }
                    else
                    {
                        Console.WriteLine($"Bound to IPv6 address: {addressIPv6}:{localPort}");
                    }
                }

                if (connectToSelf)
                {
                    Console.WriteLine("Host is connecting to itself...");
                    if (!Connect(addressIPv4, localPort))
                    {
                        Console.WriteLine("Failed to connect host to itself.");
                        return false;
                    }
                }

                Thread listenerThread = new Thread(ListenerThread);
                listenerThread.Start();

                return true;
            }
            else
            {
                if (serverAddress == null)
                {
                    Console.WriteLine("Server address must be provided in client mode.");
                    return false;
                }

                Console.WriteLine("Starting as client...");
                return Connect(serverAddress, port);
            }
        }

        /// <summary>
        /// Connect to a remote server as a client.
        /// </summary>
        public bool Connect(IPAddress serverAddress, int port)
        {
            try
            {
                _udpSocketv4 = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                _udpSocketv4.Connect(new IPEndPoint(serverAddress, port));
                IsRunning = true;

                Console.WriteLine($"Connected to server at {serverAddress}:{port}");

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

        /// <summary>
        /// Send data to the server.
        /// </summary>
        public void Send(byte[] data)
        {
            if (_udpSocketv4 == null || !IsRunning || !_udpSocketv4.Connected)
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

        private bool BindSocket(Socket socket, IPEndPoint target)
        {
            try
            {
                socket.Bind(target);
                Console.WriteLine($"Socket bound to {target}");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to bind socket to {target}: {ex.Message}");
                return false;
            }
        }

        private void ListenerThread()
        {
            EndPoint bufferEndPoint = new IPEndPoint(IPAddress.Any, 0);
            byte[] buffer = new byte[4096];

            while (IsRunning)
            {
                try
                {
                    if (_udpSocketv4 != null && _udpSocketv4.Available > 0)
                    {
                        int bytesReceived = _udpSocketv4.ReceiveFrom(buffer, ref bufferEndPoint);

                        if (bytesReceived > 0)
                        {
                            byte[] receivedData = new byte[bytesReceived];
                            Array.Copy(buffer, receivedData, bytesReceived);
                            //OnDataReceived?.Invoke(receivedData);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error in ListenerThread: {ex.Message}");
                }
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
