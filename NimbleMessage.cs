using MemoryPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
public enum MessageType
{
    ReliableOrdered, 
    Unreliable,      
    Reliable,        
    UnreliableOrdered
}
namespace NimbleNet
{
    [MemoryPackable]
    public class NetworkMessage
    {
        public MessageType Type { get; set; }
        public byte[] Data { get; set; }
    }
}
