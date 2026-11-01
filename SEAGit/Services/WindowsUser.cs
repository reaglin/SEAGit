using System;
using System.Runtime.InteropServices;
using System.Text;

namespace SEAGit.Services
{
    /// <summary>The Windows user's own name, to fill in "Name to show on GitHub" (the person can change it).</summary>
    public static class WindowsUser
    {
        [DllImport("secur32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool GetUserNameEx(int nameFormat, StringBuilder name, ref uint size);

        [DllImport("netapi32.dll", CharSet = CharSet.Unicode)]
        private static extern int NetUserGetInfo(string? server, string user, int level, out IntPtr buffer);

        [DllImport("netapi32.dll")]
        private static extern int NetApiBufferFree(IntPtr buffer);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct USER_INFO_10
        {
            public string usri10_name;
            public string usri10_comment;
            public string usri10_usr_comment;
            public string usri10_full_name;
        }

        /// <summary>
        /// The display name ("Pat Smith") when Windows has one: the account's display name, else the local account's
        /// full name (a Microsoft account sets it), else "" — never the short sign-in name, which is not a person's name.
        /// </summary>
        public static string DisplayName()
        {
            try
            {
                uint size = 256;
                var sb = new StringBuilder((int)size);
                if (GetUserNameEx(3 /* NameDisplay */, sb, ref size) && !string.IsNullOrWhiteSpace(sb.ToString()))
                    return sb.ToString().Trim();
            }
            catch (Exception ex) when (ex is DllNotFoundException or EntryPointNotFoundException) { }

            try
            {
                if (NetUserGetInfo(null, Environment.UserName, 10, out IntPtr buffer) == 0 && buffer != IntPtr.Zero)
                {
                    try
                    {
                        var info = Marshal.PtrToStructure<USER_INFO_10>(buffer);
                        if (!string.IsNullOrWhiteSpace(info.usri10_full_name)) return info.usri10_full_name.Trim();
                    }
                    finally { NetApiBufferFree(buffer); }
                }
            }
            catch (Exception ex) when (ex is DllNotFoundException or EntryPointNotFoundException) { }

            return "";
        }
    }
}
