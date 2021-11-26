using System;
using System.Net;
using System.Numerics;
using System.Collections.Generic;

namespace Tracker_Server
{
    public class IPAddressConverter
    {
        //example
        // Console.WriteLine(IPAddressConverter.ToInt(IPAddress.Parse("64.233.187.99")));
        // Console.WriteLine(IPAddressConverter.ToInt("64.233.187.99"));
        // Console.WriteLine(IPAddressConverter.FromInt(1089059683));

        public static int ToInt(string addr)
        {
            return ToInt(IPAddress.Parse(addr));
        }

        public static int ToInt(IPAddress addr)
        {
            byte[] bytes = addr.GetAddressBytes();

            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(bytes);
            }

            return BitConverter.ToInt32(bytes, 0);
        }
        
        public static BigInteger ToBigInt(string addr)
        {
            //string strIP = "2404:6800:4001:805::1006";
            IPAddress address = null;
            BigInteger ipnum = 0;

            if (IPAddress.TryParse(addr, out address))
            {
                byte[] addrBytes = address.GetAddressBytes();

                if (BitConverter.IsLittleEndian)
                {
                    List<byte> byteList = new List<byte>(addrBytes);
                    byteList.Reverse();
                    addrBytes = byteList.ToArray();
                }

                if (addrBytes.Length > 8)
                {
                    //IPv6
                    ipnum = System.BitConverter.ToUInt64(addrBytes, 8);
                    ipnum <<= 64;
                    ipnum += System.BitConverter.ToUInt64(addrBytes, 0);
                }
                else
                {
                    //IPv4
                    ipnum = System.BitConverter.ToUInt32(addrBytes, 0);
                }
            }

            return ipnum;
        }



        public static IPAddress FromInt(int IPAddr)
        {
            return IPAddress.Parse(((uint)IPAddr).ToString());
        }

        public static IPAddress FromUInt(uint IPAddr)
        {
            return IPAddress.Parse((IPAddr).ToString());
        }


        public static IPAddress FropBigInt(BigInteger IPAddr)
        {
            IPAddress address = null;

            if (IPAddress.TryParse($"{IPAddr}", out address))
            {
                
            }

            return address;
        }
    }
}
