using System;
using System.Reflection;

namespace CMPCodeDatabase.Util
{
    internal static class VersionUtil
    {
        public static string GetInformationalVersion()
        {
            var asm = typeof(VersionUtil).Assembly;
            var info = asm.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
            if (!string.IsNullOrWhiteSpace(info)) return info;
            var file = asm.GetCustomAttribute<AssemblyFileVersionAttribute>()?.Version;
            if (!string.IsNullOrWhiteSpace(file)) return file;
            return asm.GetName().Version?.ToString() ?? System.Windows.Forms.Application.ProductVersion;
        }

        public static string GetProductName()
        {
            var asm = typeof(VersionUtil).Assembly;
            var prod = asm.GetCustomAttribute<AssemblyProductAttribute>()?.Product;
            return string.IsNullOrWhiteSpace(prod) ? "CMP CodeDatabase" : prod;
        }

        /// <summary>
        /// Trim SemVer build metadata: "1.02.0+sha" -> "1.02.0".
        /// (Keeps pre-release part like "-beta.1"; remove it too by splitting on '-' first.)
        /// </summary>
        private static string TrimBuildMeta(string v)
        {
            if (string.IsNullOrWhiteSpace(v)) return v ?? string.Empty;
            return v.Split('+')[0];
        }

        public static string BuildWindowTitle(bool includeDebugSuffix = true)
        {
            var name = GetProductName();
            var ver  = TrimBuildMeta(GetInformationalVersion());
#if DEBUG
            if (includeDebugSuffix) ver += " (Debug)";
#endif
            return $"{name} {ver}";
        }
    }
}
