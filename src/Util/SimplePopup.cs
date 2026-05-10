using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;

namespace PulseDL.src.Util
{
    enum PopupTypes
    {
        Error = 0,
        Informatiive = 1,
        Success = 2
    }
    internal class SimplePopup
    {
        public async static Task<ContentDialog> ShowPopup(
            Page page,
            PopupTypes popupType,
            string message,
            string? primaryButtonText = "Quitter",
            string? secondaryButtonText = null,
            string? closeButtonText = null,
            TypedEventHandler<ContentDialog, ContentDialogButtonClickEventArgs>? primaryButtonClick = null,
            TypedEventHandler<ContentDialog, ContentDialogButtonClickEventArgs>? secondaryButtonClick = null
        )
        {
            ContentDialog dialog = new()
            {
                Title = (popupType == PopupTypes.Error ? "Une erreur est survenue" : (popupType == PopupTypes.Informatiive ? "Informative" : "Success")),
                Content = message,
                PrimaryButtonText = primaryButtonText ?? "Quitter",
                SecondaryButtonText = secondaryButtonText ?? null,
                CloseButtonText = closeButtonText ?? null,
                XamlRoot = page.XamlRoot
            };
            dialog.PrimaryButtonClick += primaryButtonClick;
            dialog.SecondaryButtonClick += secondaryButtonClick;
            await dialog.ShowAsync();
            return dialog;
        }
    }
}
