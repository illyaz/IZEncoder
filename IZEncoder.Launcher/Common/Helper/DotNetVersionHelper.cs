namespace IZEncoder.Launcher.Common.Helper
{
    using Microsoft.Win32;

    internal static class DotNetVersionHelper
    {
        public static int Get45Plus()
        {
            const string subKey = @"SOFTWARE\Microsoft\NET Framework Setup\NDP\v4\Full\";
            using (var ndpKey = RegistryKey.OpenBaseKey(RegistryHive.LocalMachine, RegistryView.Default)
                .OpenSubKey(subKey))
            {
                if (ndpKey?.GetValue("Release") == null)
                    return -1;

                var releaseKey = (int) ndpKey.GetValue("Release");
                if (releaseKey >= 461808)
                    return 472;
                if (releaseKey >= 461308)
                    return 471;
                if (releaseKey >= 460798)
                    return 470;
                if (releaseKey >= 394802)
                    return 462;
                if (releaseKey >= 394254)
                    return 461;
                if (releaseKey >= 393295)
                    return 460;
                if (releaseKey >= 379893)
                    return 452;
                if (releaseKey >= 378675)
                    return 451;
                if (releaseKey >= 378389)
                    return 450;
            }

            return -1;
        }
    }
}