using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace NetworkScanner
{
    public static class GetMacInNetwork
    {
        //http://code.613m.org/viewtopic.php?f=1&t=1882&start=10#p12621
        public static async Task<List<DeviceInNetwork>> ScanNetwork()
        {
            var listAllDevice = new List<DeviceInNetwork>();
            int counter = 0;
            foreach (var ipInter in GetInterfacesAddress())              
                foreach (var element in IpsForMask(ipInter.IPv4Mask, ipInter.Address))
                {
                    //Looking only at the first 10 addresses
                    if (counter == 10) break;
                    var res = await IpArp(element);
                    if (res != null)
                    {                       
                        var device = new DeviceInNetwork()
                        {
                            Ip = element.ToString(),
                            MacAddres = res,
                            HostName = GetHostName(element.ToString())
                        };
                        listAllDevice.Add(device);
                    }
                    counter++;
                }
            return listAllDevice;
        }


        [DllImport("iphlpapi.dll", ExactSpelling = true)]
        public static extern int SendARP(int destIp, int srcIP, byte[] macAddr, ref uint physicalAddrLen);

        public static async Task<string> IpArp(IPAddress dst)
        {
            return await Task.Run(() =>
            {
                byte[] macAddr = new byte[6];
                uint macAddrLen = (uint)macAddr.Length;

                if (SendARP(BitConverter.ToInt32(dst.GetAddressBytes(), 0), 0, macAddr, ref macAddrLen) != 0)
                    return null;

                string[] str = new string[(int)macAddrLen];
                for (int i = 0; i < macAddrLen; i++)
                    str[i] = macAddr[i].ToString("x2");

                return string.Join(":", str);
            });
        }

        public static string GetHostName(string ipAddress)
        {
            try
            {
                IPHostEntry entry = Dns.GetHostEntry(ipAddress);
                if (entry != null)
                {
                    return entry.HostName;
                }
            }
            catch (SocketException)
            {
            }

            return null;
        }

        public static IPAddress GetNetworkAddress(IPAddress address, IPAddress subnetMask)
        {
            byte[] ipAdressBytes = address.GetAddressBytes();
            byte[] subnetMaskBytes = subnetMask.GetAddressBytes();

            byte[] broadcastAddress = new byte[ipAdressBytes.Length];

            for (int i = 0; i < broadcastAddress.Length; i++)
                broadcastAddress[i] = (byte)(ipAdressBytes[i] & (subnetMaskBytes[i]));

            return new IPAddress(broadcastAddress);
        }

        public static IEnumerable<IPAddress> IpsForMask(IPAddress ip, IPAddress mask)
        {
            var first = GetNetworkAddress(ip, mask);

            var uintFirst = BitConverter.ToUInt32(first.GetAddressBytes().Reverse().ToArray(), 0);
            var uintMask = BitConverter.ToUInt32(mask.GetAddressBytes().Reverse().ToArray(), 0);

            var num = (~uintMask) - 1;

            for (uint i = 1; i < num; i++)
            {
                var reversed = BitConverter.GetBytes(uintFirst + i).Reverse().ToArray();
                yield return new IPAddress(reversed);
            }
        }

        public static IEnumerable<UnicastIPAddressInformation> GetInterfacesAddress()
        {
            foreach (var i in NetworkInterface.GetAllNetworkInterfaces())
                if (i.OperationalStatus == OperationalStatus.Up && i.NetworkInterfaceType == NetworkInterfaceType.Wireless80211)
                    foreach (var element in i.GetIPProperties().UnicastAddresses)
                        if (element.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                            yield return element;
        }
    }
    
    public class DeviceInNetwork
    {
        public string Ip { get; set; }       
        public string MacAddres { get; set; }
        public string HostName { get; set; }
    }
}
