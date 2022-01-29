namespace IZEncoder.Launcher.Common.Helper
{
    using System;
    using Microsoft.Win32;

    internal static class VisualStudioVersionHelper
    {
        public static Version Get14()
        {
            const string subKey = @"SOFTWARE\Microsoft\VisualStudio\14.0\VC\Runtimes\x64";
            using (var vcKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Default)
                .OpenSubKey(subKey))
            {
                if (vcKey == null)
                    return new Version(0, 0, 0, 0);

                if ((int) vcKey.GetValue("Installed") != 1)
                    return new Version(0, 0, 0, 0);

                return new Version((int) vcKey.GetValue("Major"), (int) vcKey.GetValue("Minor"),
                    (int) vcKey.GetValue("Bld"), (int) vcKey.GetValue("Rbld"));
            }
        }
    }
}