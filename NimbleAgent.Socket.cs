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
            //UseNativeSockets = UseNativeSockets && NativeSocket.IsSupported;
            _udpSocketv4 = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            if (!BindSocket(_udpSocketv4, new IPEndPoint(addressIPv4, port)))
                return false;

            var LocalPort = ((IPEndPoint)_udpSocketv4.LocalEndPoint).Port;

            IsRunning = true;
            //Check IPv6 support
            if (IPv6Support && IPv6Enabled)
            {
                _udpSocketv6 = new Socket(AddressFamily.InterNetworkV6, SocketType.Dgram, ProtocolType.Udp);
                //Use one port for two sockets
                if (!BindSocket(_udpSocketv6, new IPEndPoint(addressIPv6, LocalPort)))
                {
                    _udpSocketv6 = null;
                }

            }

            Thread listenerThread = new Thread(ListenerThread);
            listenerThread.Start();

            return true;

            return true;
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
                Console.WriteLine($"Failed to bind socket: {ex.Message}");
                return false;
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
