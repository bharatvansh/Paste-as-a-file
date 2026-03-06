using System;
using System.IO;
using PasteIt;
using PasteIt.Core;
using Xunit;

namespace PasteIt.Core.Tests
{
    public class PasteCommandTests : IDisposable
    {
        private readonly string _rootDirectory;

        public PasteCommandTests()
        {
            _rootDirectory = Path.Combine(
                Path.GetTempPath(),
                "PasteItCommandTests_" + Guid.NewGuid().ToString("N"));

            Directory.CreateDirectory(_rootDirectory);
        }

        [Fact]
        public void ResolveTargetDirectory_UsesConfiguredDefaultSaveLocation_WhenPreferredTargetMissing()
        {
            var configuredDirectory = Path.Combine(_rootDirectory, "configured");
            Directory.CreateDirectory(configuredDirectory);

            var result = PasteCommand.ResolveTargetDirectory(
                preferredTargetDirectory: null,
                loadSettings: () => new AppSettings { DefaultSaveLocation = configuredDirectory });

            Assert.Equal(Path.GetFullPath(configuredDirectory), result);
        }

        [Fact]
        public void ResolveTargetDirectory_PrefersExplicitTarget_OverConfiguredDefault()
        {
            var preferredDirectory = Path.Combine(_rootDirectory, "preferred");
            var configuredDirectory = Path.Combine(_rootDirectory, "configured");
            Directory.CreateDirectory(preferredDirectory);
            Directory.CreateDirectory(configuredDirectory);

            var result = PasteCommand.ResolveTargetDirectory(
                preferredTargetDirectory: preferredDirectory,
                loadSettings: () => new AppSettings { DefaultSaveLocation = configuredDirectory });

            Assert.Equal(Path.GetFullPath(preferredDirectory), result);
        }

        public void Dispose()
        {
            if (Directory.Exists(_rootDirectory))
            {
                Directory.Delete(_rootDirectory, recursive: true);
            }
        }
    }
}
