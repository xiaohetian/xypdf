using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Net.NetworkInformation;
using System.Security.Cryptography;
using System.Text;

namespace LicenseTool.Helpers
{
    /// <summary>
    /// 生成基于硬件的唯一标识，用于许可证验证
    /// 使用多个硬件特征的组合生成稳定的硬件指纹
    /// </summary>
    public static class HardwareIdHelper
    {
        /// <summary>
        /// 生成硬件唯一标识（硬件指纹）
        /// </summary>
        /// <returns>硬件指纹字符串</returns>
        public static string GenerateHardwareId()
        {
            try
            {
                var components = new List<string>();

                // 获取CPU ID
                var cpuId = GetWmiProperty("Win32_Processor", "ProcessorId");
                if (!string.IsNullOrEmpty(cpuId))
                {
                    components.Add($"CPU:{cpuId.Trim()}");
                }

                // 获取主板序列号
                var baseBoardSerial = GetWmiProperty("Win32_BaseBoard", "SerialNumber");
                if (!string.IsNullOrEmpty(baseBoardSerial))
                {
                    components.Add($"BASEBOARD:{baseBoardSerial.Trim()}");
                }

                // 获取BIOS序列号
                var biosSerial = GetWmiProperty("Win32_BIOS", "SerialNumber");
                if (!string.IsNullOrEmpty(biosSerial))
                {
                    components.Add($"BIOS:{biosSerial.Trim()}");
                }

                // 组合所有可用的组件
                var combined = string.Join("|", components.Where(c => !string.IsNullOrEmpty(c)));

                // 如果我们有足够的硬件信息，使用它生成哈希
                if (!string.IsNullOrEmpty(combined) && components.Count >= 2)
                {
                    using (var sha256 = SHA256.Create())
                    {
                        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(combined));
                        // 返回16个字符的十六进制哈希
                        return BitConverter.ToString(hashBytes).Replace("-", "").Substring(0, 16);
                    }
                }
                else
                {
                    // 硬件信息不足时，尝试使用MAC地址作为补充
                    var macAddresses = GetMacAddresses();
                    if (macAddresses.Any())
                    {
                        // 将MAC地址添加到组件中
                        foreach (var mac in macAddresses.Take(3)) // 只取前3个MAC地址避免过长
                        {
                            components.Add($"MAC:{mac}");
                        }
                        
                        var macCombined = string.Join("|", components.Where(c => !string.IsNullOrEmpty(c)));
                        if (!string.IsNullOrEmpty(macCombined))
                        {
                            using (var sha256 = SHA256.Create())
                            {
                                var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(macCombined));
                                return BitConverter.ToString(hashBytes).Replace("-", "").Substring(0, 16);
                            }
                        }
                    }
                }

                // 最后的回退：使用时间戳（仅在所有其他方法都失败时使用）
                return $"FALLBACK:{DateTime.Now:yyyyMMddHHmmss}";
            }
            catch
            {
                // 如果出现任何错误，返回基于时间的ID
                return $"ERROR:{DateTime.Now:yyyyMMddHHmmss}";
            }
        }

        /// <summary>
        /// 通过WMI查询获取属性值
        /// </summary>
        private static string GetWmiProperty(string wmiClass, string propertyName, string condition = null)
        {
            try
            {
                using (var searcher = new ManagementObjectSearcher(
                    string.IsNullOrEmpty(condition) 
                        ? $"SELECT {propertyName} FROM {wmiClass}" 
                        : $"SELECT {propertyName} FROM {wmiClass} WHERE {condition}"))
                {
                    using (var collection = searcher.Get())
                    {
                        foreach (var obj in collection)
                        {
                            var value = obj[propertyName];
                            if (value != null && !string.IsNullOrEmpty(value.ToString().Trim()))
                            {
                                return value.ToString().Trim();
                            }
                        }
                    }
                }
            }
            catch
            {
                // WMI查询可能失败（特别是在没有管理员权限或某些系统配置下）
                // 静默处理并返回null
            }
            return null;
        }

        /// <summary>
        /// 获取所有有效的网络接口MAC地址
        /// </summary>
        private static IEnumerable<string> GetMacAddresses()
        {
            var macList = new List<string>();
            try
            {
                foreach (var nic in NetworkInterface.GetAllNetworkInterfaces())
                {
                    // 只考虑已启用、不是环回、不是虚拟的真实网络接口
                    if (nic.OperationalStatus == OperationalStatus.Up &&
                        nic.NetworkInterfaceType != NetworkInterfaceType.Loopback &&
                        nic.NetworkInterfaceType != NetworkInterfaceType.Tunnel &&
                        nic.NetworkInterfaceType != NetworkInterfaceType.Ppp &&
                        nic.GetPhysicalAddress().GetAddressBytes().Length > 0)
                    {
                        // 检查是否是虚拟网卡（常见的虚拟网卡前缀或描述）
                        var description = nic.Description?.ToUpper() ?? "";
                        var name = nic.Name?.ToUpper() ?? "";
                        
                        // 排除常见的虚拟网卡关键词
                        var isVirtual = description.Contains("VIRTUAL") || 
                                       description.Contains("MICROSOFT") || 
                                       description.Contains("LOOPBACK") ||
                                       name.Contains("VIRTUAL") ||
                                       name.Contains("VPN") ||
                                       name.Contains("VBOX") ||
                                       name.Contains("VMWARE");
                                        
                        if (!isVirtual)
                        {
                            var mac = nic.GetPhysicalAddress();
                            var macString = BitConverter.ToString(mac.GetAddressBytes());
                            if (!string.IsNullOrEmpty(macString))
                            {
                                macList.Add(macString);
                            }
                        }
                    }
                }
            }
            catch
            {
                // 忽略MAC获取错误
            }
            return macList;
        }
    }
}