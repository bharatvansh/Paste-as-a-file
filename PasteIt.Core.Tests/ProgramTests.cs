using System.ComponentModel;
using System.Windows.Forms;
using PasteIt;
using Xunit.Sdk;
using Xunit;

namespace PasteIt.Core.Tests
{
    public class ProgramTests
    {
        [Fact]
        public void RunService_ReturnsSuccess_WhenApplicationRunnerCompletes()
        {
            var ran = false;

            var result = Program.RunService(
                contextFactory: () => new ApplicationContext(),
                runApplication: context => ran = context != null);

            Assert.Equal(0, result);
            Assert.True(ran);
        }

        [Fact]
        public void RunService_ReportsFriendlyMessage_WhenHotkeyRegistrationFails()
        {
            string? reportedMessage = null;

            var result = Program.RunService(
                contextFactory: () => throw new Win32Exception(1409, "Unable to register global hotkey."),
                reportError: message => reportedMessage = message,
                runApplication: _ => throw new XunitException("Application runner should not be called when context creation fails."));

            Assert.Equal(1, result);
            Assert.Equal(
                "PasteIt couldn't start because Ctrl+Shift+V is already in use by another app or PasteIt instance.",
                reportedMessage);
        }
    }
}
