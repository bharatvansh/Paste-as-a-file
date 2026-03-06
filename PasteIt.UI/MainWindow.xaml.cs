using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using PasteIt.Core;

namespace PasteIt.UI
{
    public partial class MainWindow : Window
    {
        private readonly HistoryManager _historyManager = new HistoryManager();
        private readonly SettingsManager _settingsManager = new SettingsManager();

        public MainWindow()
        {
            InitializeComponent();
            LoadHistory();
            LoadSettings();

            // If launched with --view settings, switch to Settings tab.
            if (string.Equals(App.RequestedView, "settings", System.StringComparison.OrdinalIgnoreCase))
            {
                BtnSettings_Click(this, new RoutedEventArgs());
            }
        }

        // --- Navigation ---

        private void BtnHistory_Click(object sender, RoutedEventArgs e)
        {
            HistoryView.Visibility = Visibility.Visible;
            SettingsView.Visibility = Visibility.Collapsed;
            BtnHistory.Tag = "active";
            BtnSettings.Tag = null;
            LoadHistory();
        }

        private void BtnSettings_Click(object sender, RoutedEventArgs e)
        {
            HistoryView.Visibility = Visibility.Collapsed;
            SettingsView.Visibility = Visibility.Visible;
            BtnHistory.Tag = null;
            BtnSettings.Tag = "active";
        }

        // --- History ---

        private void LoadHistory()
        {
            var entries = _historyManager.GetEntries();

            if (entries.Count == 0)
            {
                EmptyHistoryLabel.Visibility = Visibility.Visible;
                HistoryList.ItemsSource = null;
                return;
            }

            EmptyHistoryLabel.Visibility = Visibility.Collapsed;
            HistoryList.ItemsSource = entries.Select(e => new HistoryItemViewModel(e)).ToList();
        }

        private void HistoryCard_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement el && el.DataContext is HistoryItemViewModel vm)
            {
                vm.TogglePreview();
            }
        }

        private void CopyButton_Click(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;

            if (sender is FrameworkElement el && el.DataContext is HistoryItemViewModel vm)
            {
                try
                {
                    if (!string.IsNullOrEmpty(vm.Entry.PreviewText))
                    {
                        Clipboard.SetText(vm.Entry.PreviewText);
                    }
                    else if (File.Exists(vm.Entry.FilePath))
                    {
                        var col = new System.Collections.Specialized.StringCollection();
                        col.Add(vm.Entry.FilePath);
                        Clipboard.SetFileDropList(col);
                    }
                    else
                    {
                        Clipboard.SetText(vm.Entry.FilePath);
                    }
                }
                catch { }
            }
        }

        // --- Settings ---

        private void LoadSettings()
        {
            var s = _settingsManager.Load();
            TxtMaxHistory.Text = s.MaxHistoryItems.ToString();
            TxtFilenamePrefix.Text = s.FilenamePrefix;
            TxtDefaultSaveLocation.Text = s.DefaultSaveLocation ?? string.Empty;
            TxtFfmpegPath.Text = s.FfmpegPath ?? string.Empty;
            ChkAutoStart.IsChecked = s.AutoStartOnBoot;
        }

        private void SaveSettings_Click(object sender, MouseButtonEventArgs e)
        {
            var settings = new AppSettings();

            if (int.TryParse(TxtMaxHistory.Text, out var max) && max > 0)
                settings.MaxHistoryItems = max;

            settings.FilenamePrefix = string.IsNullOrWhiteSpace(TxtFilenamePrefix.Text)
                ? "clipboard" : TxtFilenamePrefix.Text.Trim();

            settings.DefaultSaveLocation = string.IsNullOrWhiteSpace(TxtDefaultSaveLocation.Text)
                ? null : TxtDefaultSaveLocation.Text.Trim();

            settings.FfmpegPath = string.IsNullOrWhiteSpace(TxtFfmpegPath.Text)
                ? null : TxtFfmpegPath.Text.Trim();

            settings.AutoStartOnBoot = ChkAutoStart.IsChecked == true;

            _settingsManager.Save(settings);

            MessageBox.Show("Settings saved.", "PasteIt", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ClearHistory_Click(object sender, MouseButtonEventArgs e)
        {
            var result = MessageBox.Show(
                "Clear all paste history?", "PasteIt",
                MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                _historyManager.ClearHistory();
                LoadHistory();
            }
        }
    }

    // --- ViewModel ---

    public class HistoryItemViewModel : INotifyPropertyChanged
    {
        public HistoryEntry Entry { get; }
        private bool _showPreview;

        public HistoryItemViewModel(HistoryEntry entry) => Entry = entry;

        public string FileName => Path.GetFileName(Entry.FilePath);

        public string TypeLabel
        {
            get
            {
                switch (Entry.ContentType)
                {
                    case "Image": return "IMG";
                    case "Audio": return "AUD";
                    case "Video": return "VID";
                    case "Url": return "URL";
                    case "Html": return "HTML";
                    case "Code": return "CODE";
                    case "Text": return "TXT";
                    default: return "FILE";
                }
            }
        }

        public string TimeAgo
        {
            get
            {
                var span = DateTime.UtcNow - Entry.TimestampUtc;
                if (span.TotalMinutes < 1) return "just now";
                if (span.TotalMinutes < 60) return $"{(int)span.TotalMinutes}m ago";
                if (span.TotalHours < 24) return $"{(int)span.TotalHours}h ago";
                if (span.TotalDays < 7) return $"{(int)span.TotalDays}d ago";
                return Entry.TimestampUtc.ToLocalTime().ToString("MMM dd, yyyy");
            }
        }

        public string FileSize
        {
            get
            {
                var b = Entry.FileSizeBytes;
                if (b < 1024) return $"{b} B";
                if (b < 1024 * 1024) return $"{b / 1024.0:F1} KB";
                return $"{b / (1024.0 * 1024.0):F1} MB";
            }
        }

        public string PreviewText
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(Entry.PreviewText))
                {
                    return Entry.PreviewText!;
                }

                switch (Entry.ContentType)
                {
                    case "Image":
                        return "(Image file)";
                    case "Audio":
                        return "(Audio file)";
                    case "Video":
                        return "(Video file)";
                    default:
                        return "(No preview available)";
                }
            }
        }

        public Visibility PreviewVisibility => _showPreview ? Visibility.Visible : Visibility.Collapsed;

        public void TogglePreview()
        {
            _showPreview = !_showPreview;
            OnPropertyChanged(nameof(PreviewVisibility));
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
