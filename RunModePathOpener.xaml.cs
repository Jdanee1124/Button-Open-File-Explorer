using System;
using System.Diagnostics;
using System.IO;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using DIAVision.UI.Base;
using DIAVision.UI.Unified.DiaControl;

namespace PluginSamples
{
    public class RunModePathOpener : DiaControlPlugin
    {
        public RunModePathOpener()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private async void OnOpenPath(object sender, RoutedEventArgs e)
        {
            var txtPath = this.FindControl<TextBox>("TxtPath");
            var path = txtPath?.Text?.Trim();

            if (string.IsNullOrEmpty(path))
            {
                await ShowMessage("提示", "请先输入文件夹路径。");
                return;
            }

            if (!Directory.Exists(path))
            {
                await ShowMessage("错误", $"路径不存在：{path}");
                return;
            }

            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = path,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                await ShowMessage("打开失败", ex.Message);
            }
        }

        private static async System.Threading.Tasks.Task ShowMessage(string title, string message)
        {
            var mainWindow = ((IClassicDesktopStyleApplicationLifetime)
                Application.Current.ApplicationLifetime).MainWindow;
            await DIAVision.UI.Base.Tools.MessageBox.Create(title, message)
                .ShowDialog(mainWindow)
                .ConfigureAwait(false);
        }

        protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
        {
        }
    }
}
