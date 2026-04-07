using System;
using System.Drawing;
using System.IO;
using System.Reflection;
#if NET5_0_OR_GREATER
using System.Runtime.Versioning;
#endif

namespace PasteIt.Core
{
#if NET5_0_OR_GREATER
    [SupportedOSPlatform("windows")]
#endif
    public static class LogoProvider
    {
        public static Image? GetLogo()
        {
            try
            {
                var assembly = Assembly.GetExecutingAssembly();
                const string resourceName = "PasteIt.Core.Resources.logo.png";

                using (var stream = assembly.GetManifestResourceStream(resourceName))
                {
                    if (stream == null)
                    {
                        return null;
                    }
                    return Image.FromStream(stream);
                }
            }
            catch
            {
                return null;
            }
        }
    }
}
