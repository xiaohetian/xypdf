using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;

namespace PdfToolbox.Helpers
{
    public static class MacAddressHelper
    {
        /// <summary>
        /// 获取本机所有网络接口的MAC地址（排除无效和虚拟接口）
        /// </summary>
        /// <returns>MAC地址列表，格式如 "1A-47-C1-5D-10-34"</returns>
        public static List<string> GetMacAddresses()
        {
            var macAddresses = new List<string>();
            foreach (var nic in NetworkInterface.GetAllNetworkInterfaces())
            {
                // 只考虑已启用且不是环回的接口
                if (nic.OperationalStatus == OperationalStatus.Up &&
                    nic.NetworkInterfaceType != NetworkInterfaceType.Loopback &&
                    nic.GetPhysicalAddress().GetAddressBytes().Length > 0)
                {
                    var mac = nic.GetPhysicalAddress();
                    // 格式化为XX-XX-XX-XX-XX-XX（大写）
                    var macString = BitConverter.ToString(mac.GetAddressBytes());
                    if (!string.IsNullOrEmpty(macString))
                    {
                        macAddresses.Add(macString);
                    }
                }
            }
            return macAddresses.Distinct().ToList();
        }
    }
}