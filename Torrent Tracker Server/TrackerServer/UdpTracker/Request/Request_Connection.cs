using System;
using System.Threading.Tasks;
using DotNetty.Common.Utilities;

namespace Tracker_Server.UdpTracker
{
    public class Request_Connection : ClientRequest
    {
        public const long PROTOCOL_ID = 0x41727101980L;

        public override void parsePacket()
        {
            if (this.connectionId != PROTOCOL_ID)
            {
                Response_Error.send(this.getDatagram().Sender, this.getContext(), 
                    this.getTransactionId(), "잘못된 protocol.");
                return;
            }

            Random random = new Random();

            do
            {
                //클라이언트별로 고유 connectionID 등록.
                this.connectionId = random.NextLong();
            } while (this.connectionId == PROTOCOL_ID);

            
            Response_Connection.send(this.getDatagram().Sender, this.getContext(), this.getTransactionId(), this.getConnectionId());
        }
    }
}
