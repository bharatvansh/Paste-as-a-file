using System;
using System.Reflection;

namespace PasteIt.Core
{
    public static class AppVersionInfo
    {
        public static string GetDisplayVersion(Assembly? assembly = null)
        {
            var informationalVersion = GetInformationalVersion(assembly);
            if (!string.IsNullOrWhiteSpace(informationalVersion))
            {
                return informationalVersion!;
            }

            var version = GetVersion(assembly);
            return version?.ToString(3) ?? "0.0.0";
        }

        public static Version? GetVersion(Assembly? assembly = null)
        {
            var targetAssembly = assembly ?? Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
            return targetAssembly.GetName().Version;
        }

        private static string? GetInformationalVersion(Assembly? assembly)
        {
            var targetAssembly = assembly ?? Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly();
            return targetAssembly
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                ?.InformationalVersion;
        }
    }
}
