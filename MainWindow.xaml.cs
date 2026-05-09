using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Windowing;
using WinRT.Interop;
using PulseDL.src.Pages;
using System;
using System.Collections.Generic;
using System.IO;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Microsoft.UI;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using PulseDL.src.Managers;
using PulseDL.src.Types;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace PulseDL
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        internal Settings settings = SettingsManager.Load();
        public MainWindow()
        {
            InitializeComponent();
            var hwnd = WindowNative.GetWindowHandle(this);
            var windowId = Win32Interop.GetWindowIdFromWindow(hwnd);
            var appWindow = AppWindow.GetFromWindowId(windowId);
            appWindow.SetPresenter(AppWindowPresenterKind.Overlapped);
            appWindow.TitleBar.ExtendsContentIntoTitleBar = true;
            if(appWindow.Presenter is OverlappedPresenter presenter)
            {
                presenter.PreferredMinimumWidth = 1200;
                presenter.PreferredMinimumHeight = 650;
            }
            ExtendsContentIntoTitleBar = true;
            Init();
        }

        private async void Init()
        {
            if (!await YtdlpManager.IsYtdlpInstalled() || !await FfmpegManager.IsFfmpegInstalled())
            {
                NavView.SelectedItem = NavView.SettingsItem;
                MainFrame.Navigate(typeof(SettingsPage));
            }
            else
            {
                NavView.SelectedItem = NavView.MenuItems.First();
                MainFrame.Navigate(typeof(DownloadVideoPage));
            }
        }

        private readonly Dictionary<string, Type> pages = new()
        {
            { "DownloadVideoPage", typeof(DownloadVideoPage) }
        };

        private bool IsCurrentPageSettings = false;
        private void NavigationView_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
        {
            if(args.IsSettingsInvoked == true)
            {
                MainFrame.Navigate(typeof(SettingsPage));
                IsCurrentPageSettings = true;
                return;
            }
            if (args.InvokedItemContainer != null)
            {
                if(IsCurrentPageSettings)
                {
                    IsCurrentPageSettings = false;
                }
                string tag = args.InvokedItemContainer.Tag.ToString();
                if(pages.TryGetValue(tag, out var pageType))
                {
                    MainFrame.Navigate(pageType);
                }
            }
        }
    }
}