namespace IZEncoder.Launcher.Common.Helper
{
    using System;
    using System.Text.RegularExpressions;

    internal static class AvisynthVersionHelper
    {
        public static bool IsPlus()
        {
            var env = IntPtr.Zero;
            try
            {
                env = NativeAvs.create_script_environment(6);
                var v = ((string) NativeAvs.as_string(NativeAvs.invoke(env, "VersionString",
                    NativeAvs.new_value_array(null),
                    null))).ToLower();
                Console.WriteLine(v);
                return v.Contains("avisynth+") || v.Contains("avisynthplus") || v.Contains("avisynth plus");
            }
            catch (EntryPointNotFoundException)
            {
                return false;
            }
            catch (DllNotFoundException)
            {
                return false;
            }
            finally
            {
                if (env != IntPtr.Zero)
                    NativeAvs.delete_script_environment(env);
            }
        }

        public static int GetPlus()
        {
            var env = IntPtr.Zero;
            try
            {
                env = NativeAvs.create_script_environment(6);
                var v = (string) NativeAvs.as_string(NativeAvs.invoke(env, "VersionString",
                    NativeAvs.new_value_array(null),
                    null));

                var rex = new Regex(@"r(?<version>\d+)");
                var m = rex.Match(v);
                if (m.Success && int.TryParse(m.Groups["version"].ToString(), out var version))
                    return version;
            }
            catch (EntryPointNotFoundException) { }
            catch (DllNotFoundException) { }
            finally
            {
                if (env != IntPtr.Zero)
                    NativeAvs.delete_script_environment(env);
            }

            return -1;
        }
    }
}