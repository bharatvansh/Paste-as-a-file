using System;
using NAudio.Wave;
using NAudio.MediaFoundation;
using System.IO;

class Program
{
    static void Main(string[] args)
    {
        MediaFoundationApi.Startup();
        var wavPath = "test.wav";
        
        // create a dummy 1-second wav file
        var waveFormat = new WaveFormat(44100, 16, 2);
        using (var writer = new WaveFileWriter(wavPath, waveFormat))
        {
            var bytes = new byte[44100 * 2 * 2];
            writer.Write(bytes, 0, bytes.Length);
        }

        using (var reader = new WaveFileReader(wavPath))
        {
            Console.WriteLine("Encoding to AAC...");
            MediaFoundationEncoder.EncodeToAac(reader, "test.aac");
            Console.WriteLine("AAC works.");
        }

        using (var reader = new WaveFileReader(wavPath))
        {
            Console.WriteLine("Encoding to FLAC...");
            MediaFoundationEncoder.EncodeToAudioType(reader, "test.flac", AudioSubtypes.MFAudioFormat_FLAC);
            Console.WriteLine("FLAC works.");
        }
    }
}
