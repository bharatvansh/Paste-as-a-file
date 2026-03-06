using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.IO;
using System.Text;
using NAudio.Lame;
using NAudio.Wave;
using NAudio.MediaFoundation;
using OggVorbisEncoder;
using FlacBox;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Webp;

namespace PasteIt.Core
{
    public sealed class FileSaver
    {
        private static readonly System.Text.Encoding _encoding = new System.Text.UTF8Encoding(false);

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
                
            var path = GenerateUniquePath(directory, extension, now ?? DateTime.Now);

            switch (content.Type)
            {
                case ClipboardContentType.Image:
                    SaveImage(content, path);
                    break;
                case ClipboardContentType.Audio:
                    SaveAudio(content, path);
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
            if (!extension.StartsWith("."))
            {
                extension = "." + extension;
            }

            for (var counter = 1; counter <= 999; counter++)
            {
                var filename = $"clipboard_{now:yyyy-MM-dd}_{counter:D3}{extension}";
                var fullPath = Path.Combine(targetDirectory, filename);
                if (!File.Exists(fullPath))
                {
                    return fullPath;
                }
            }

            return Path.Combine(
                targetDirectory,
                $"clipboard_{now:yyyy-MM-dd}_{Guid.NewGuid():N}{extension}");
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
                case ClipboardContentType.Text:
                    return ".txt";
                default:
                    return ".txt";
            }
        }

        private static string BuildDisplayType(ClipboardContent content, string extension)
        {
            switch (content.Type)
            {
                case ClipboardContentType.Image:
                    return $"Image ({extension})";
                case ClipboardContentType.Audio:
                    return $"Audio ({extension})";
                case ClipboardContentType.Url:
                    return "URL (.url)";
                case ClipboardContentType.Html:
                    return "HTML (.html)";
                case ClipboardContentType.Code:
                    return $"{content.SuggestedLanguage ?? "Code"} ({extension})";
                case ClipboardContentType.Text:
                    return "Text (.txt)";
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

            var ext = Path.GetExtension(path);
            content.AudioContent.Position = 0;

            if (string.Equals(ext, ".mp3", StringComparison.OrdinalIgnoreCase))
            {
                using (var reader = new WaveFileReader(content.AudioContent))
                using (var writer = new LameMP3FileWriter(path, reader.WaveFormat, LAMEPreset.STANDARD))
                {
                    reader.CopyTo(writer);
                }
            }
            else if (string.Equals(ext, ".aac", StringComparison.OrdinalIgnoreCase))
            {
                MediaFoundationApi.Startup();
                using (var reader = new WaveFileReader(content.AudioContent))
                {
                    MediaFoundationEncoder.EncodeToAac(reader, path);
                }
            }
            else if (string.Equals(ext, ".flac", StringComparison.OrdinalIgnoreCase))
            {
                using (var fs = new FileStream(path, FileMode.Create, FileAccess.Write))
                using (var flacStream = new WaveOverFlacStream(fs, WaveOverFlacStreamMode.Encode))
                {
                    content.AudioContent.CopyTo(flacStream);
                }
            }
            else if (string.Equals(ext, ".ogg", StringComparison.OrdinalIgnoreCase))
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

            // WebP requires SixLabors.ImageSharp since System.Drawing doesn't support it.
            if (string.Equals(ext, ".webp", StringComparison.OrdinalIgnoreCase))
            {
                SaveAsWebP(content.ImageContent, path);
                return;
            }

            var format = ResolveImageFormat(ext);

            if (IsJpeg(ext))
            {
                var encoder = GetEncoder(ImageFormat.Jpeg);
                if (encoder != null)
                {
                    var encoderParams = new EncoderParameters(1);
                    encoderParams.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, 95L);
                    content.ImageContent.Save(path, encoder, encoderParams);
                    return;
                }
            }

            content.ImageContent.Save(path, format);
        }

        private static void SaveAsWebP(System.Drawing.Image image, string path)
        {
            using (var ms = new MemoryStream())
            {
                image.Save(ms, ImageFormat.Png);
                ms.Position = 0;

                using (var sharpImage = SixLabors.ImageSharp.Image.Load(ms))
                {
                    sharpImage.SaveAsWebp(path, new WebpEncoder { Quality = 90 });
                }
            }
        }

        private static ImageFormat ResolveImageFormat(string extension)
        {
            var map = new Dictionary<string, ImageFormat>(StringComparer.OrdinalIgnoreCase)
            {
                { ".png", ImageFormat.Png },
                { ".jpg", ImageFormat.Jpeg },
                { ".jpeg", ImageFormat.Jpeg },
                { ".bmp", ImageFormat.Bmp },
                { ".gif", ImageFormat.Gif },
                { ".tiff", ImageFormat.Tiff },
                { ".tif", ImageFormat.Tiff },
                { ".ico", ImageFormat.Icon },
            };

            return map.TryGetValue(extension ?? string.Empty, out var fmt) ? fmt : ImageFormat.Png;
        }

        private static bool IsJpeg(string extension)
        {
            return string.Equals(extension, ".jpg", StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(extension, ".jpeg", StringComparison.OrdinalIgnoreCase);
        }

        private static ImageCodecInfo? GetEncoder(ImageFormat format)
        {
            foreach (var codec in ImageCodecInfo.GetImageEncoders())
            {
                if (codec.FormatID == format.Guid)
                {
                    return codec;
                }
            }
            return null;
        }

        private static void SaveUrlShortcut(ClipboardContent content, string path)
        {
            var body = "[InternetShortcut]" + Environment.NewLine +
                       "URL=" + (content.TextContent ?? string.Empty);
            File.WriteAllText(path, body, System.Text.Encoding.ASCII);
        }
    }
}

