using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using PulseDL.src.Managers;
using PulseDL.src.Types;
using PulseDL.src.Util;
using Windows.ApplicationModel.DataTransfer;
using Windows.Devices.PointOfService;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.System;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace PulseDL.src.Pages
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class DownloadVideoPage : Page
    {
        public DownloadVideoPage()
        {
            InitializeComponent();
            CheckForUpdates();
            Init();
        }

        private bool DisableSearchButton = false;

        private async void Init()
        {
            if (!(await FfmpegManager.IsFfmpegInstalled()) || !(await YtdlpManager.IsYtdlpInstalled()))
            {
                DisableSearchButton = true;
                SearchButton.IsEnabled = false;
                ToolTip tooltip = new()
                {
                    Content = "Impossible de lancer la recherche car Yt-dlp ou Ffmpeg n'est pas installé. Vérifiez la page des paramètres."
                };
                ToolTipService.SetToolTip(SearchButtonGrid, tooltip);
                Debug.WriteLine("testgds");
                return;
            }
        }

        private async Task CheckForUpdates()
        {
            LatestVersionInfo latestVersion = await UpdateManager.GetLatestVersionInfo();
            if (latestVersion == null)
            {
                Debug.WriteLine("Failed to check for updates.");
                return;
            }
            var version = Assembly
                .GetExecutingAssembly()
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?
                .InformationalVersion;
            if (latestVersion.Core.Version != version)
            {
                Button updateButton = new()
                {
                    Content = "Mettre à jour"
                };
                updateButton.Click += (s, e) =>
                {
                    UpdateManager.InstallLatestVersion(latestVersion);
                };
                InfoBar updateInfoBar = new()
                {
                    Title = "Mise à jour disponible",
                    IsOpen = true,
                    Severity = InfoBarSeverity.Informational,
                    Margin = new Thickness(0, 20, 0, 0),
                    Message = $"Une nouvelle version de PulseDL est disponible (v{latestVersion.Core.Version}). Cela téléchargera la nouvelle version et redémarrera PulseDL automatiquement. Voulez-vous mettre PulseDL à jour ?",
                    ActionButton = updateButton,
                };
                MainStackPanel.Children.Insert(0, updateInfoBar);
            }
        }

        private YoutubeVideoData currentVideoData = new YoutubeVideoData();

        private void UrlInput_TextChanged(object sender, TextChangedEventArgs e)
        {
            if(string.IsNullOrEmpty(UrlInput.Text.Trim()))
            {
                SearchButton.IsEnabled = false;
            } else
            {
                SearchButton.IsEnabled = true;
            }
            if(DisableSearchButton)
            {
                SearchButton.IsEnabled = false;
            }
        }

        private void UrlInput_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == VirtualKey.Enter)
            {
                SearchButton_Click(null, null);
                e.Handled = true;
            }
                
        }

        private async void PasteButton_Click(object sender, RoutedEventArgs e)
        {
            var package = Clipboard.GetContent();
            if (package.Contains(StandardDataFormats.Text))
            {
                var text = await package.GetTextAsync();
                UrlInput.Text = text;
            }
        }

        private async void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            string url = UrlInput.Text;
            SearchButton.IsEnabled = false;
            SearchProgressRing.IsActive = true;
            YoutubeVideoData video = await YtdlpManager.GetVideoData(url);
            SearchButton.IsEnabled = true;
            SearchProgressRing.IsActive = false;
            VideoTitle.Text = video.title;
            VideoThumbnail.UriSource = new Uri(video.thumbnail ?? "https://static.vecteezy.com/system/resources/thumbnails/004/141/669/small/no-photo-or-blank-image-icon-loading-images-or-missing-image-mark-image-not-available-or-image-coming-soon-sign-simple-nature-silhouette-in-frame-isolated-illustration-vector.jpg");
            List<AudioFormatItem> audioFormats = [];
            List<VideoFormatItem> videoFormats = [];
            audioFormats.Add(
                new AudioFormatItem
                {
                    format = new YoutubeFormat
                    {
                        format_id = "empty"
                    },
                    IsEmpty = true
                }
            );
            videoFormats.Add(
                new VideoFormatItem
                {
                    format = new YoutubeFormat
                    {
                        format_id = "empty"
                    },
                    IsEmpty = true
                }
            );
            currentVideoData = video;
            if(video.formats != null && video.formats.Count != 0)
            {
                foreach (YoutubeFormat format in video.formats)
                {
                    if (format.ext == "mhtml") continue;
                    if (format.protocol == "m3u8") continue;
                    if (format.filesize == null) continue;
                    if (format.resolution == "audio only" || (format.vcodec == "audio only" && !string.IsNullOrEmpty(format.acodec)))
                    {
                        if (format.abr == null || format.asr == null) continue;
                        audioFormats.Add(new AudioFormatItem
                        {
                            format = format,
                            IsEmpty = false
                        });
                    }
                    if (format.resolution != "audio only" || (format.acodec == "video only" && !string.IsNullOrEmpty(format.vcodec)))
                    {
                        if (format.width == null || format.height == null || format.vbr == null) continue;
                        videoFormats.Add(new VideoFormatItem
                        {
                            format = format,
                            IsEmpty = false
                        });
                    }
                }
            }
            AudioDropdownAudioChoice.ItemsSource = audioFormats;
            VideoDropdownVideoChoice.ItemsSource = videoFormats;

            VideoDetailsScrollView.Visibility = Visibility.Visible;
        }

        private async void UpdateDownloadButtonState()
        {
            VideoFormatItem selectedVideoFormat = (VideoFormatItem)VideoDropdownVideoChoice.SelectedItem;
            AudioFormatItem selectedAudioFormat = (AudioFormatItem)AudioDropdownAudioChoice.SelectedItem;
            if (selectedVideoFormat == null || selectedAudioFormat == null)
            {
                DownloadButton.IsEnabled = false;
                return;
            }
            if(selectedAudioFormat.IsEmpty && selectedVideoFormat.IsEmpty)
            {
                DownloadButton.IsEnabled = false;
                return;
            }
            if((selectedAudioFormat.IsEmpty && !selectedVideoFormat.IsEmpty) || (selectedVideoFormat.IsEmpty && !selectedAudioFormat.IsEmpty) || (!selectedAudioFormat.IsEmpty && !selectedVideoFormat.IsEmpty))
            {
                DownloadButton.IsEnabled = true;
            } else
            {
                DownloadButton.IsEnabled = false;
            }
        }

        private void VideoDropdownVideoChoice_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateDownloadButtonState();
        }

        private void AudioDropdownAudioChoice_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateDownloadButtonState();
        }

        private async void DownloadButton_Click(object sender, RoutedEventArgs e)
        {
            currentVideoData.title = Sanitizer.SanitizeFileName(Sanitizer.RemoveEmojis(currentVideoData.title));
            VideoFormatItem selectedVideoFormat = (VideoFormatItem)VideoDropdownVideoChoice.SelectedItem;
            AudioFormatItem selectedAudioFormat = (AudioFormatItem)AudioDropdownAudioChoice.SelectedItem;
            DownloadButton.IsEnabled = false;
            ProgressBar downloadingProgress = new() { IsIndeterminate = true };
            TextBlock downloadStep = new() { Text = "Initialisation du téléchargement...", Margin = new Thickness(0, 0, 0, 10) };
            ContentDialog downloadingDialog = new()
            {
                Title = "Téléchargement",
                Width = 850,
                Height = 400,
                Content = new StackPanel
                {
                    Children =
                    {
                        new TextBlock{ Text = "Téléchargement de votre vidéo en cours..." },
                        downloadStep,
                        new TextBlock{ Text = $"Titre : {currentVideoData.title}" },
                        new TextBlock{ Text = $"Qualité vidéo : {selectedVideoFormat.Display}" },
                        new TextBlock{ Text = $"Qualité audio : {selectedAudioFormat.Display}", Margin = new Thickness(0, 0, 0, 20) },
                        downloadingProgress
                    }
                },
                PrimaryButtonText = "Lancer la vidéo",
                CloseButtonText = null,
                SecondaryButtonText = "Ouvrir le dossier",
                IsPrimaryButtonEnabled = false,
                IsSecondaryButtonEnabled = false,
                XamlRoot = this.Content.XamlRoot
            };
            var showTask = downloadingDialog.ShowAsync();
            string stringFormat = "";
            if(selectedVideoFormat.IsEmpty)
            {
                stringFormat = selectedAudioFormat.format.format_id;
            } else if (selectedAudioFormat.IsEmpty)
            {
                stringFormat = selectedVideoFormat.format.format_id;
            } else
            {
                stringFormat = $"{selectedVideoFormat.format.format_id}+{selectedAudioFormat.format.format_id}";
            }
            string finalFilepath = "";
            await YtdlpManager.DownloadYoutubeVideo(
                currentVideoData,
                stringFormat,
                (progress) =>
                {
                    this.DispatcherQueue.TryEnqueue(() =>
                    {
                        downloadingProgress.IsIndeterminate = false;
                        downloadingProgress.Value = Math.Round(progress);
                    });
                },
                (milestoneName, arg) =>
                {
                    if(milestoneName == "dl_part" && stringFormat.Contains("+"))
                    {
                        this.DispatcherQueue.TryEnqueue(() =>
                        {
                            downloadStep.Text = $"Téléchargement de {arg}";
                        });
                    }
                    if(milestoneName == "dl_final" && !stringFormat.Contains("+"))
                    {
                        finalFilepath = arg;
                        this.DispatcherQueue.TryEnqueue(() =>
                        {
                            downloadStep.Text = $"Téléchargement de {arg}";
                        });
                    }
                    if(milestoneName == "merging" && stringFormat.Contains("+"))
                    {
                        finalFilepath = arg;
                        this.DispatcherQueue.TryEnqueue(() =>
                        {
                            downloadStep.Text = $"Fusion des formats dans {arg}";
                        });
                    }
                }
            );
            downloadingDialog.Content = new StackPanel
            {
                Children =
                    {
                        new TextBlock{ Text = "Téléchargement terminé !" },
                        new TextBlock{ Text = $"Enregistré en tant que {finalFilepath}" }
                    }
            };
            downloadingDialog.IsPrimaryButtonEnabled = true;
            downloadingDialog.IsSecondaryButtonEnabled = true;
            downloadingDialog.CloseButtonText = "Fermer";
            downloadingDialog.CloseButtonClick += (_, __) =>
            {
                showTask.Cancel();
            };
            downloadingDialog.PrimaryButtonClick += (_, __) =>
            {
                try
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = finalFilepath,
                        UseShellExecute = true,
                        Verb = "open"
                    });
                }
                catch (Exception ex)
                {
                    ContentDialog errorDialog = new ContentDialog
                    {
                        Title = "Erreur",
                        Content = $"Impossible d'ouvrir le fichier : {ex.Message}",
                        CloseButtonText = "Fermer",
                        XamlRoot = this.Content.XamlRoot
                    };
                    errorDialog.ShowAsync();
                }
            };
            downloadingDialog.SecondaryButtonClick += (_, __) =>
            {
                try
                {
                    Process.Start("explorer.exe", "/select,\"" + finalFilepath + "\"");
                }
                catch (Exception ex)
                {
                    ContentDialog errorDialog = new ContentDialog
                    {
                        Title = "Erreur",
                        Content = $"Impossible d'ouvrir le fichier : {ex.Message}",
                        CloseButtonText = "Fermer",
                        XamlRoot = this.Content.XamlRoot
                    };
                    errorDialog.ShowAsync();
                }
            };
            DownloadButton.IsEnabled = true;
        }
    }
}
