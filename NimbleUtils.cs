using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MemoryPack;

namespace NimbleNet
{
    internal class NimbleUtils
    {
        public NetworkMessage DeserializeMessage(byte[] data)
        {
            return MemoryPackSerializer.Deserialize<NetworkMessage>(data);
        }
        public byte[] SerializeMessage(NetworkMessage message)
        {
            return MemoryPackSerializer.Serialize(message);
        }
    }
}
