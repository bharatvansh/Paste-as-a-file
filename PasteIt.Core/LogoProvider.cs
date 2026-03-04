using System;
using System.Drawing;
using System.IO;
using System.Reflection;

namespace PasteIt.Core
{
    public static class LogoProvider
    {
        public static Image GetLogo()
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
