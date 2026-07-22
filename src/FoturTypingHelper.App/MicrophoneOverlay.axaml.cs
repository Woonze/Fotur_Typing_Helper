using Avalonia.Controls;

namespace FoturTypingHelper.App;

public partial class MicrophoneOverlay : Window
{
    public MicrophoneOverlay() => InitializeComponent();
    public void SetProcessing()
    {
        OverlayTitle.Text = "Распознаю…";
        OverlaySubtitle.Text = "Локальная модель Whisper";
    }
}
