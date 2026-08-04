using System;
using System.IO;
using System.Windows;
using LicenseTool.Helpers;

namespace LicenseTool
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            GenerateAndDisplayFingerprint();
        }

        private void GenerateAndDisplayFingerprint()
        {
            try
            {
                string fingerprint = HardwareIdHelper.GenerateHardwareId();
                FingerprintTextBox.Text = fingerprint;
                StatusText.Text = "已生成机器指纹码，请复制上方的字符串";
                StatusText.Foreground = System.Windows.Media.Brushes.Green;
            }
            catch (Exception ex)
            {
                FingerprintTextBox.Text = "生成失败";
                StatusText.Text = $"生成指纹时出错：{ex.Message}";
                StatusText.Foreground = System.Windows.Media.Brushes.Red;
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string fingerprint = FingerprintTextBox.Text.Trim();
                if (string.IsNullOrEmpty(fingerprint) || fingerprint.Length < 10)
                {
                    MessageBox.Show("请先生成有效的机器指纹码", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
                string pdfToolboxPath = Path.Combine(appDataPath, "PdfToolbox");
                string securityJsonPath = Path.Combine(pdfToolboxPath, "security.json");

                // 确保目录存在
                if (!Directory.Exists(pdfToolboxPath))
                {
                    Directory.CreateDirectory(pdfToolboxPath);
                }

                // 创建或更新security.json文件
                var securitySettings = new
                {
                    AuthorizedMacAddresses = new string[] { fingerprint }
                };

                string json = System.Text.Json.JsonSerializer.Serialize(securitySettings, new System.Text.Json.JsonSerializerOptions
                {
                    WriteIndented = true
                });

                File.WriteAllText(securityJsonPath, json);

                StatusText.Text = $"许可证已保存到：{securityJsonPath}";
                StatusText.Foreground = System.Windows.Media.Brushes.Green;
                MessageBox.Show($"许可证已成功保存！\n\n路径：{securityJsonPath}\n\n请将此文件复制到目标电脑的相同位置。", "保存成功", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                StatusText.Text = $"保存许可证时出错：{ex.Message}";
                StatusText.Foreground = System.Windows.Media.Brushes.Red;
                MessageBox.Show($"保存许可证时出错：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}