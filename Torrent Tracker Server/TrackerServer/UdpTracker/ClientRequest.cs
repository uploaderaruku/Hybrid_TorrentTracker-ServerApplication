using System.Threading.Tasks;
using DotNetty.Buffers;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Sockets;

namespace Tracker_Server.UdpTracker
{
    public abstract class ClientRequest
    {
        private IChannelHandlerContext ctx;
        private DatagramPacket datagram;


        protected long connectionId;
        protected Action action;
        protected int transactionId;

        public abstract void parsePacket();


        public IChannelHandlerContext getContext() { return ctx; }
        public void setContext(IChannelHandlerContext ctx) { this.ctx = ctx; }

        public IByteBuffer getByteBuffer() { return this.datagram.Content; }
        public DatagramPacket getDatagram() { return this.datagram; }
        public void setDatagram(DatagramPacket datagram) { this.datagram = datagram; }


        public long getConnectionId() { return connectionId; }
        public void setConnectionId(long connectionId) { this.connectionId = connectionId; }

        public Action getAction() { return action; }
        public void setAction(Action action) { this.action = action; }

        public int getTransactionId() { return transactionId; }
        public void setTransactionId(int transactionId) { this.transactionId = transactionId; }
    }
}
