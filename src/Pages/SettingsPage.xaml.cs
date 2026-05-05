using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using Windows.Storage.Pickers;
using WinRT.Interop;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using System.Threading.Tasks;
using PulseDL.Pages;
using PulseDL.src;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace PulseDL.src.Pages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class SettingsPage : Page
    {
        internal Settings settings = SettingsManager.Load();
        public SettingsPage()
        {
            InitializeComponent();
            init();
            if(settings.DownloadPath != null)
            {
                DownloadFolder.Content = settings.DownloadPath;
            }
        }

        private async void init()
        {
            if (await YtdlpManager.isYtdlpInstalled())
            {
                InstallYtdlp.Content = "Yt-dlp est déja installé";
                InstallYtdlp.IsEnabled = false;
            }
        }

        private async void DownloadFolder_Click(object sender, RoutedEventArgs e)
        {
            FolderPicker picker = new FolderPicker();
            var hwnd = WindowNative.GetWindowHandle(App.MainWindow);
            InitializeWithWindow.Initialize(picker, hwnd);
            picker.FileTypeFilter.Add("*");
            var folder = await picker.PickSingleFolderAsync();
            if (folder != null) {
                settings.DownloadPath = folder.Path;
                SettingsManager.Save(settings);
                DownloadFolder.Content = folder.Path;
            }
        }

        private async void InstallYtdlp_Click(object sender, RoutedEventArgs e)
        {
            if (await YtdlpManager.isYtdlpInstalled())
            {
                InstallYtdlp.Content = "Yt-dlp est déja installé";
                InstallYtdlp.IsEnabled = false;
                return;
            }

            ContentDialog downloadingDialog = new ContentDialog
            {
                Title = "Téléchargement de yt-dlp",
                Content = new StackPanel
                {
                    Children =
                    {
                        new TextBlock{ Text = "Téléchargement de yt-dlp en cours...", Margin = new Thickness(0,0,0,10) },
                        new ProgressRing{ IsActive = true, Width = 40, Height = 40 }
                    }
                },
                PrimaryButtonText = "Quitter",
                CloseButtonText = null,
                IsPrimaryButtonEnabled = false,
                IsSecondaryButtonEnabled = false,
                XamlRoot = this.Content.XamlRoot
            };

            var showTask = downloadingDialog.ShowAsync();
            await YtdlpManager.DownloadYtdlp();
            downloadingDialog.Content = new TextBlock
            {
                Text = "Installation terminée !"
            };
            downloadingDialog.IsPrimaryButtonEnabled = true;
            downloadingDialog.PrimaryButtonClick += (_, __) =>
            {
                showTask.Cancel();
            };
            InstallYtdlp.Content = "Yt-dlp est déja installé";
            InstallYtdlp.IsEnabled = false;
        }
    }
}
