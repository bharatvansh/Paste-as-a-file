using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing.Imaging;
using System.IO;
using System.Text;
using NAudio.Lame;
using NAudio.Wave;
using NAudio.MediaFoundation;
using OggVorbisEncoder;
using FlacBox;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Bmp;
using SixLabors.ImageSharp.Formats.Gif;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.Formats.Tiff;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.PixelFormats;

namespace PasteIt.Core
{
    public sealed class FileSaver
    {
        private static readonly System.Text.Encoding _encoding = new System.Text.UTF8Encoding(false);
        private readonly Func<AppSettings> _loadSettings;
        private readonly VideoTranscodeHandler _videoTranscodeHandler;

        public delegate void VideoTranscodeHandler(Stream videoStream, string sourceExtension, string outputPath, Func<AppSettings> loadSettings);

        public FileSaver(Func<AppSettings>? loadSettings = null, VideoTranscodeHandler? videoTranscodeHandler = null)
        {
            _loadSettings = loadSettings ?? (() => new SettingsManager().Load());
            _videoTranscodeHandler = videoTranscodeHandler ?? ConvertVideoWithFfmpeg;
        }

        public FileSaveResult Save(ClipboardContent content, string? targetDirectory, DateTime? now = null, string? extensionOverride = null)
        {
            if (content == null)
            {
                throw new ArgumentNullException(nameof(content));
            }

            if (!content.HasContent)
            {
                throw new InvalidOperationException("Clipboard content is empty.");
            }

            var directory = ResolveTargetDirectory(targetDirectory);
            
            // If an extension override is provided, use it. Otherwise, fallback to old default logic for backwards compatibility.
            var extension = !string.IsNullOrWhiteSpace(extensionOverride) 
                ? extensionOverride! 
                : ResolveExtension(content);
            extension = NormalizeExtension(extension);
                
            var path = GenerateUniquePath(directory, extension, now ?? DateTime.Now);

            switch (content.Type)
            {
                case ClipboardContentType.Image:
                    SaveImage(content, path);
                    break;
                case ClipboardContentType.Audio:
                    SaveAudio(content, path);
                    break;
                case ClipboardContentType.Video:
                    SaveVideo(content, path, extension);
                    break;
                case ClipboardContentType.Url:
                    SaveUrlShortcut(content, path);
                    break;
                case ClipboardContentType.Html:
                case ClipboardContentType.Code:
                case ClipboardContentType.Text:
                    SaveText(content, path);
                    break;
                default:
                    throw new InvalidOperationException($"Unsupported clipboard type: {content.Type}.");
            }

            return new FileSaveResult(path, BuildDisplayType(content, extension));
        }

        public string GenerateUniquePath(string targetDirectory, string extension, DateTime now)
        {
            extension = NormalizeExtension(extension);
            var prefix = ResolveFilenamePrefix();

            for (var counter = 1; counter <= 999; counter++)
            {
                var filename = $"{prefix}_{now:yyyy-MM-dd}_{counter:D3}{extension}";
                var fullPath = Path.Combine(targetDirectory, filename);
                if (!File.Exists(fullPath))
                {
                    return fullPath;
                }
            }

            return Path.Combine(
                targetDirectory,
                $"{prefix}_{now:yyyy-MM-dd}_{Guid.NewGuid():N}{extension}");
        }

        private static string ResolveTargetDirectory(string? preferredPath)
        {
            if (!string.IsNullOrWhiteSpace(preferredPath))
            {
                try
                {
                    var fullPath = Path.GetFullPath(preferredPath);
                    Directory.CreateDirectory(fullPath);
                    return fullPath;
                }
                catch
                {
                }
            }

            var desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            Directory.CreateDirectory(desktop);
            return desktop;
        }

        private static string ResolveExtension(ClipboardContent content)
        {
            // Fallback resolver. The UI/ShellExtension will mostly pass the explicit extension from now on.
            switch (content.Type)
            {
                case ClipboardContentType.Image:
                    return ".png";
                case ClipboardContentType.Url:
                    return ".url";
                case ClipboardContentType.Html:
                    return ".html";
                case ClipboardContentType.Code:
                    return string.IsNullOrWhiteSpace(content.SuggestedExtension) ? ".txt" : content.SuggestedExtension!;
                case ClipboardContentType.Audio:
                    return string.IsNullOrWhiteSpace(content.SuggestedExtension) ? ".wav" : content.SuggestedExtension!;
                case ClipboardContentType.Video:
                    return string.IsNullOrWhiteSpace(content.SuggestedExtension) ? ".mp4" : content.SuggestedExtension!;
                case ClipboardContentType.Text:
                    return ".txt";
                default:
                    return ".txt";
            }
        }

        private string ResolveFilenamePrefix()
        {
            try
            {
                var prefix = _loadSettings().FilenamePrefix;
                return string.IsNullOrWhiteSpace(prefix) ? "clipboard" : prefix.Trim();
            }
            catch
            {
                return "clipboard";
            }
        }

        private static string NormalizeExtension(string extension)
        {
            return extension.StartsWith(".") ? extension : "." + extension;
        }

        private static string BuildDisplayType(ClipboardContent content, string extension)
        {
            switch (content.Type)
            {
                case ClipboardContentType.Image:
                    return $"Image ({extension})";
                case ClipboardContentType.Audio:
                    return $"Audio ({extension})";
                case ClipboardContentType.Video:
                    return $"Video ({extension})";
                case ClipboardContentType.Url:
                    return "URL (.url)";
                case ClipboardContentType.Html:
                    return $"HTML ({extension})";
                case ClipboardContentType.Code:
                    return $"{content.SuggestedLanguage ?? "Code"} ({extension})";
                case ClipboardContentType.Text:
                    return $"Text ({extension})";
                default:
                    return $"File ({extension})";
            }
        }

        private void SaveText(ClipboardContent content, string path)
        {
            File.WriteAllText(path, content.TextContent ?? string.Empty, _encoding);
        }

        private static void SaveAudio(ClipboardContent content, string path)
        {
            if (content.AudioContent == null)
            {
                throw new InvalidOperationException("Audio content is missing.");
            }

            var targetExtension = NormalizeExtension(Path.GetExtension(path));
            var sourceExtension = NormalizeExtension(content.SuggestedExtension ?? ".wav");

            if (content.AudioContent.CanSeek)
            {
                content.AudioContent.Position = 0;
            }

            if (!string.Equals(sourceExtension, ".wav", StringComparison.OrdinalIgnoreCase))
            {
                if (!string.Equals(sourceExtension, targetExtension, StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException(
                        $"Audio clipboard data can only be saved as {sourceExtension} because conversion from the copied source format is not supported.");
                }

                using (var fs = new FileStream(path, FileMode.Create, FileAccess.Write))
                {
                    content.AudioContent.CopyTo(fs);
                }

                return;
            }

            if (string.Equals(targetExtension, ".mp3", StringComparison.OrdinalIgnoreCase))
            {
                using (var reader = new WaveFileReader(content.AudioContent))
                using (var writer = new LameMP3FileWriter(path, reader.WaveFormat, LAMEPreset.STANDARD))
                {
                    reader.CopyTo(writer);
                }
            }
            else if (string.Equals(targetExtension, ".aac", StringComparison.OrdinalIgnoreCase))
            {
                MediaFoundationApi.Startup();
                using (var reader = new WaveFileReader(content.AudioContent))
                {
                    MediaFoundationEncoder.EncodeToAac(reader, path);
                }
            }
            else if (string.Equals(targetExtension, ".flac", StringComparison.OrdinalIgnoreCase))
            {
                using (var fs = new FileStream(path, FileMode.Create, FileAccess.Write))
                using (var flacStream = new WaveOverFlacStream(fs, WaveOverFlacStreamMode.Encode))
                {
                    content.AudioContent.CopyTo(flacStream);
                }
            }
            else if (string.Equals(targetExtension, ".ogg", StringComparison.OrdinalIgnoreCase))
            {
                using (var reader = new WaveFileReader(content.AudioContent))
                {
                    var sampleProvider = reader.ToSampleProvider();
                    
                    var vorbisInfo = VorbisInfo.InitVariableBitRate(reader.WaveFormat.Channels, reader.WaveFormat.SampleRate, 0.5f);
                    var vState = ProcessingState.Create(vorbisInfo);

                    using (var fs = new FileStream(path, FileMode.Create, FileAccess.Write))
                    {
                        var oggStream = new OggStream(1);
                        var bufferObjects = new float[reader.WaveFormat.Channels][];
                        var readBuffer = new float[reader.WaveFormat.SampleRate * reader.WaveFormat.Channels]; // 1s buffer

                        for (int i = 0; i < reader.WaveFormat.Channels; i++)
                        {
                            bufferObjects[i] = new float[reader.WaveFormat.SampleRate];
                        }

                        int samplesRead;
                        while ((samplesRead = sampleProvider.Read(readBuffer, 0, readBuffer.Length)) > 0)
                        {
                            int frames = samplesRead / reader.WaveFormat.Channels;
                            for (int c = 0; c < reader.WaveFormat.Channels; c++)
                            {
                                for (int i = 0; i < frames; i++)
                                {
                                    bufferObjects[c][i] = readBuffer[i * reader.WaveFormat.Channels + c];
                                }
                            }
                            vState.WriteData(bufferObjects, frames);

                            FlushVorbis(vState, oggStream, fs);
                        }
                        vState.WriteEndOfStream();
                        FlushVorbis(vState, oggStream, fs);
                    }
                }
            }
            else
            {
                // Uncompressed WAV
                using (var fs = new FileStream(path, FileMode.Create, FileAccess.Write))
                {
                    content.AudioContent.CopyTo(fs);
                }
            }
        }

        private void SaveVideo(ClipboardContent content, string path, string requestedExtension)
        {
            if (content.VideoContent == null)
            {
                throw new InvalidOperationException("Video content is missing.");
            }

            var normalizedRequestedExtension = VideoConversionSupport.NormalizeExtension(requestedExtension);
            var sourceExtension = VideoConversionSupport.NormalizeExtension(content.SuggestedExtension ?? normalizedRequestedExtension);

            if (!string.Equals(normalizedRequestedExtension, sourceExtension, StringComparison.OrdinalIgnoreCase))
            {
                _videoTranscodeHandler(content.VideoContent, sourceExtension, path, _loadSettings);
                return;
            }

            if (content.VideoContent.CanSeek)
            {
                content.VideoContent.Position = 0;
            }

            using (var output = new FileStream(path, FileMode.Create, FileAccess.Write))
            {
                content.VideoContent.CopyTo(output);
            }
        }

        private static void ConvertVideoWithFfmpeg(Stream videoStream, string sourceExtension, string outputPath, Func<AppSettings> loadSettings)
        {
            var ffmpegPath = VideoConversionSupport.ResolveFfmpegPath(loadSettings);
            if (string.IsNullOrWhiteSpace(ffmpegPath))
            {
                throw new InvalidOperationException(
                    "Video conversion requires FFmpeg. Add ffmpeg.exe to PATH or set it in Settings.");
            }

            var normalizedSourceExtension = VideoConversionSupport.NormalizeExtension(sourceExtension);
            var normalizedOutputExtension = VideoConversionSupport.NormalizeExtension(Path.GetExtension(outputPath));
            if (!VideoConversionSupport.IsSupportedExtension(normalizedSourceExtension) ||
                !VideoConversionSupport.IsSupportedExtension(normalizedOutputExtension))
            {
                throw new InvalidOperationException("The selected video format is not supported for conversion.");
            }

            var tempInputPath = Path.Combine(
                Path.GetTempPath(),
                "pasteit_video_" + Guid.NewGuid().ToString("N") + normalizedSourceExtension);

            try
            {
                if (videoStream.CanSeek)
                {
                    videoStream.Position = 0;
                }

                using (var tempInput = new FileStream(tempInputPath, FileMode.Create, FileAccess.Write))
                {
                    videoStream.CopyTo(tempInput);
                }

                var startInfo = new ProcessStartInfo
                {
                    FileName = ffmpegPath,
                    Arguments = BuildFfmpegArguments(tempInputPath, outputPath),
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardError = true,
                    RedirectStandardOutput = true
                };

                using (var process = Process.Start(startInfo))
                {
                    if (process == null)
                    {
                        throw new InvalidOperationException("Unable to start FFmpeg for video conversion.");
                    }

                    var standardOutput = process.StandardOutput.ReadToEnd();
                    var standardError = process.StandardError.ReadToEnd();
                    process.WaitForExit();

                    if (process.ExitCode != 0 || !File.Exists(outputPath))
                    {
                        TryDelete(outputPath);

                        var details = string.IsNullOrWhiteSpace(standardError) ? standardOutput : standardError;
                        throw new InvalidOperationException(
                            "FFmpeg could not convert the video." +
                            (string.IsNullOrWhiteSpace(details) ? string.Empty : " " + details.Trim()));
                    }
                }
            }
            finally
            {
                TryDelete(tempInputPath);
            }
        }

        private static string BuildFfmpegArguments(string inputPath, string outputPath)
        {
            var outputExtension = VideoConversionSupport.NormalizeExtension(Path.GetExtension(outputPath));
            var extraArguments = string.Equals(outputExtension, ".mp4", StringComparison.OrdinalIgnoreCase) ||
                                 string.Equals(outputExtension, ".m4v", StringComparison.OrdinalIgnoreCase)
                ? " -movflags +faststart"
                : string.Empty;

            return $"-hide_banner -loglevel error -y -i {QuoteArgument(inputPath)}{extraArguments} {QuoteArgument(outputPath)}";
        }

        private static string QuoteArgument(string value)
        {
            return "\"" + (value ?? string.Empty).Replace("\"", "\\\"") + "\"";
        }

        private static void TryDelete(string? path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return;
            }

            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
            catch
            {
            }
        }

        private static void FlushVorbis(ProcessingState vState, OggStream oggStream, Stream fs)
        {
            while (!oggStream.Finished && vState.PacketOut(out OggPacket packet))
            {
                oggStream.PacketIn(packet);
                while (!oggStream.Finished && oggStream.PageOut(out OggPage page, false))
                {
                    fs.Write(page.Header, 0, page.Header.Length);
                    fs.Write(page.Body, 0, page.Body.Length);
                }
            }
        }

        private static void SaveImage(ClipboardContent content, string path)
        {
            if (content.ImageContent == null)
            {
                throw new InvalidOperationException("Image content is missing.");
            }

            var ext = Path.GetExtension(path);
            if (string.Equals(ext, ".ico", StringComparison.OrdinalIgnoreCase))
            {
                SaveIcon(content.ImageContent, path);
                return;
            }

            using (var image = LoadImageSharpImage(content.ImageContent))
            {
                SaveWithImageSharp(image, ext, path);
            }
        }

        private static Image<Rgba32> LoadImageSharpImage(System.Drawing.Image image)
        {
            using (var ms = new MemoryStream())
            {
                image.Save(ms, ImageFormat.Png);
                ms.Position = 0;
                return SixLabors.ImageSharp.Image.Load<Rgba32>(ms);
            }
        }

        private static void SaveWithImageSharp(Image<Rgba32> image, string extension, string path)
        {
            if (string.Equals(extension, ".png", StringComparison.OrdinalIgnoreCase))
            {
                image.Save(path, new PngEncoder());
                return;
            }

            if (IsJpeg(extension))
            {
                image.Save(path, new JpegEncoder { Quality = 95 });
                return;
            }

            if (string.Equals(extension, ".webp", StringComparison.OrdinalIgnoreCase))
            {
                image.Save(path, new WebpEncoder { Quality = 90 });
                return;
            }

            if (string.Equals(extension, ".bmp", StringComparison.OrdinalIgnoreCase))
            {
                image.Save(path, new BmpEncoder());
                return;
            }

            if (string.Equals(extension, ".gif", StringComparison.OrdinalIgnoreCase))
            {
                image.Save(path, new GifEncoder());
                return;
            }

            if (string.Equals(extension, ".tiff", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(extension, ".tif", StringComparison.OrdinalIgnoreCase))
            {
                image.Save(path, new TiffEncoder());
                return;
            }

            image.Save(path, new PngEncoder());
        }

        private static void SaveIcon(System.Drawing.Image image, string path)
        {
            var format = ResolveImageFormat(".ico");
            image.Save(path, format);
        }

        private static ImageFormat ResolveImageFormat(string extension)
        {
            var map = new Dictionary<string, ImageFormat>(StringComparer.OrdinalIgnoreCase)
            {
                { ".ico", ImageFormat.Icon },
            };

            return map.TryGetValue(extension ?? string.Empty, out var fmt) ? fmt : ImageFormat.Png;
        }

        private static bool IsJpeg(string extension)
        {
            return string.Equals(extension, ".jpg", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(extension, ".jpeg", StringComparison.OrdinalIgnoreCase);
        }

        private static void SaveUrlShortcut(ClipboardContent content, string path)
        {
            var body = "[InternetShortcut]" + Environment.NewLine +
                       "URL=" + (content.TextContent ?? string.Empty);
            File.WriteAllText(path, body, System.Text.Encoding.ASCII);
        }
    }
}
