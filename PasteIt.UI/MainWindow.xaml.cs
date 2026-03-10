using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows;
using System.Windows.Input;
using PasteIt.Core;

namespace PasteIt.UI
{
    public partial class MainWindow : Window
    {
        private readonly HistoryManager _historyManager = new HistoryManager();
        private readonly SettingsManager _settingsManager = new SettingsManager();
        private List<HistoryItemViewModel> _historyItems = new List<HistoryItemViewModel>();

        public MainWindow()
        {
            InitializeComponent();
            Title = "PasteIt v" + AppVersionInfo.GetDisplayVersion();
            TxtSidebarVersion.Text = "v" + AppVersionInfo.GetDisplayVersion();
            LoadHistory();
            LoadSettings();

            // If launched with --view settings, switch to Settings tab.
            if (string.Equals(App.RequestedView, "settings", System.StringComparison.OrdinalIgnoreCase))
            {
                BtnSettings_Click(this, new RoutedEventArgs());
            }
        }

        protected override void OnActivated(EventArgs e)
        {
            base.OnActivated(e);

            if (HistoryView.Visibility == Visibility.Visible)
            {
                LoadHistory();
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
            _historyItems = entries.Select(e => new HistoryItemViewModel(e)).ToList();
            RefreshHistoryView();
        }

        private void HistoryCard_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement el && el.DataContext is HistoryItemViewModel vm)
            {
                if (!EnsureEntryExists(vm))
                {
                    return;
                }

                vm.TogglePreview();
            }
        }

        private void CopyButton_Click(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;

            if (sender is FrameworkElement el && el.DataContext is HistoryItemViewModel vm)
            {
                if (!EnsureEntryExists(vm))
                {
                    return;
                }

                TryRunHistoryAction(() =>
                {
                    ClipboardAccessor.Execute<object?>(() =>
                    {
                        Clipboard.Clear();

                        if (!string.IsNullOrEmpty(vm.CopyText))
                        {
                            Clipboard.SetText(vm.CopyText);
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

                        Clipboard.Flush();
                        return null;
                    });
                });
            }
        }

        private void RevealButton_Click(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;

            if (sender is FrameworkElement el && el.DataContext is HistoryItemViewModel vm)
            {
                if (!EnsureEntryExists(vm))
                {
                    return;
                }

                TryRunHistoryAction(() =>
                {
                    var directoryPath = Path.GetDirectoryName(vm.Entry.FilePath);
                    if (string.IsNullOrWhiteSpace(directoryPath) || !Directory.Exists(directoryPath))
                    {
                        throw new DirectoryNotFoundException("The saved file's folder no longer exists.");
                    }

                    Process.Start(new ProcessStartInfo
                    {
                        FileName = directoryPath,
                        UseShellExecute = true
                    });
                });
            }
        }

        private void RemoveButton_Click(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;

            if (!(sender is FrameworkElement el && el.DataContext is HistoryItemViewModel vm))
            {
                return;
            }

            _historyManager.DeleteEntry(vm.Entry);
            LoadHistory();
        }

        private void RefreshHistoryView()
        {
            HistoryList.ItemsSource = _historyItems;

            if (_historyItems.Count == 0)
            {
                EmptyHistoryLabel.Text = "Nothing here yet. Paste something with Ctrl+Shift+V or the right-click menu.";
                EmptyHistoryLabel.Visibility = Visibility.Visible;
                return;
            }

            EmptyHistoryLabel.Visibility = Visibility.Collapsed;
        }

        private static void TryRunHistoryAction(Action action)
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "PasteIt", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private bool EnsureEntryExists(HistoryItemViewModel vm)
        {
            if (vm == null)
            {
                return false;
            }

            if (File.Exists(vm.Entry.FilePath))
            {
                return true;
            }

            _historyManager.DeleteEntry(vm.Entry);
            LoadHistory();
            return false;
        }

        // --- Settings ---

        private void LoadSettings()
        {
            var s = _settingsManager.Load();
            TxtMaxHistory.Text = s.MaxHistoryItems.ToString();
            TxtFilenamePrefix.Text = s.FilenamePrefix;
            TxtDefaultSaveLocation.Text = s.DefaultSaveLocation ?? string.Empty;
            TxtFfmpegPath.Text = s.FfmpegPath ?? string.Empty;
            ChkEnableHistory.IsChecked = s.EnableHistory;
            ChkEnableAutoUpdates.IsChecked = s.EnableAutoUpdates;
            TxtCurrentVersion.Text = "Current version: v" + AppVersionInfo.GetDisplayVersion();
            TxtLastUpdateCheck.Text = "Last checked: " + FormatUpdateCheckTime(s.LastUpdateCheckUtc);
            TxtUpdateStatus.Text = "Updates are checked from GitHub Releases.";
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

            settings.EnableHistory = ChkEnableHistory.IsChecked != false;
            settings.EnableAutoUpdates = ChkEnableAutoUpdates.IsChecked != false;

            var existingSettings = _settingsManager.Load();
            settings.LastUpdateCheckUtc = existingSettings.LastUpdateCheckUtc;
            settings.SkippedVersion = existingSettings.SkippedVersion;

            _settingsManager.Save(settings);

            MessageBox.Show("Settings saved.", "PasteIt", MessageBoxButton.OK, MessageBoxImage.Information);
            LoadSettings();
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

        private async void CheckForUpdates_Click(object sender, MouseButtonEventArgs e)
        {
            TxtUpdateStatus.Text = "Checking GitHub Releases for updates...";

            try
            {
                var result = await System.Threading.Tasks.Task.Run(() => new UpdateChecker().CheckForUpdates(manualCheck: true));
                LoadSettings();

                if (!string.IsNullOrWhiteSpace(result.ErrorMessage))
                {
                    TxtUpdateStatus.Text = "Update check failed: " + result.ErrorMessage;
                    MessageBox.Show(result.ErrorMessage, "PasteIt", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (!result.IsUpdateAvailable || result.UpdateInfo == null)
                {
                    TxtUpdateStatus.Text = "You're up to date.";
                    MessageBox.Show("You're already on the latest version.", "PasteIt", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                TxtUpdateStatus.Text = "PasteIt " + result.UpdateInfo.VersionString + " is available.";

                var settings = _settingsManager.Load();
                if (settings.EnableAutoUpdates)
                {
                    var autoResult = MessageBox.Show(
                        "PasteIt " + result.UpdateInfo.VersionString + " is available.\n\nInstall it now?",
                        "PasteIt Update Available",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Information);

                    if (autoResult == MessageBoxResult.Yes)
                    {
                        new UpdateChecker().ClearSkippedVersion();
                        await System.Threading.Tasks.Task.Run(() =>
                        {
                            var installer = new UpdateInstaller();
                            var installerPath = installer.DownloadInstaller(result.UpdateInfo);
                            installer.LaunchInstaller(installerPath, silent: true);
                        });
                        Application.Current.Shutdown();
                    }

                    return;
                }

                var manualResult = MessageBox.Show(
                    "PasteIt " + result.UpdateInfo.VersionString + " is available.\n\nYes = Download and open installer\nNo = Later\nCancel = Skip this version",
                    "PasteIt Update Available",
                    MessageBoxButton.YesNoCancel,
                    MessageBoxImage.Information);

                if (manualResult == MessageBoxResult.Cancel)
                {
                    new UpdateChecker().SkipVersion(result.UpdateInfo.VersionString);
                    LoadSettings();
                    TxtUpdateStatus.Text = "This version will be skipped until a newer release is published.";
                    return;
                }

                if (manualResult == MessageBoxResult.Yes)
                {
                    new UpdateChecker().ClearSkippedVersion();
                    await System.Threading.Tasks.Task.Run(() =>
                    {
                        var installer = new UpdateInstaller();
                        var installerPath = installer.DownloadInstaller(result.UpdateInfo);
                        installer.LaunchInstaller(installerPath, silent: false);
                    });
                    Application.Current.Shutdown();
                }
            }
            catch (Exception ex)
            {
                TxtUpdateStatus.Text = "Update check failed: " + ex.Message;
                MessageBox.Show(ex.Message, "PasteIt", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private static string FormatUpdateCheckTime(DateTime? lastCheckedUtc)
        {
            if (!lastCheckedUtc.HasValue)
            {
                return "Never";
            }

            return lastCheckedUtc.Value.ToLocalTime().ToString("g");
        }

    }

    // --- ViewModel ---

    public class HistoryItemViewModel : INotifyPropertyChanged
    {
        public HistoryEntry Entry { get; }
        private bool _showPreview;

        public HistoryItemViewModel(HistoryEntry entry) => Entry = entry;

        public string FileName => Path.GetFileName(Entry.FilePath);

        public string? CopyText => !string.IsNullOrEmpty(Entry.FullText)
            ? Entry.FullText
            : Entry.PreviewText;

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
