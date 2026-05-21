namespace SEAGit
{
    internal static class Program
    {
        /// <summary>
        /// Crash log path under the user's local app data — writable under MSIX
        /// (the install folder is read-only). Written with primitive File I/O so
        /// it works even before/while WinForms is starting up.
        /// </summary>
        private static string CrashLogPath =>
            Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "SEAGit", "startup-crash.log");

        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            // Install crash handlers FIRST so a launch-time exception is logged
            // and shown rather than terminating the process silently. The Store
            // certification lab saw only a 0xe0434352 (unhandled managed
            // exception) crash with no further detail; this captures the cause.
            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            {
                if (e.ExceptionObject is Exception ex)
                    WriteCrashLog("Unhandled domain exception", ex);
            };

            Application.ThreadException += (s, e) =>
            {
                WriteCrashLog("Unhandled UI thread exception", e.Exception);
                try
                {
                    MessageBox.Show(
                        "SEAGit hit an unexpected error.\n\n" +
                        e.Exception.Message + "\n\n" +
                        "A crash log was written to:\n" + CrashLogPath,
                        "SEAGit — Unexpected Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                catch { /* ignore */ }
            };

            try
            {
                ApplicationConfiguration.Initialize();
                Application.Run(new MainForm());
            }
            catch (Exception ex)
            {
                WriteCrashLog("SEAGit startup failed", ex);
                try
                {
                    MessageBox.Show(
                        "SEAGit could not start.\n\n" + ex.Message + "\n\n" +
                        "A crash log was written to:\n" + CrashLogPath,
                        "SEAGit — Startup Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                catch { /* nothing more we can do */ }
            }
        }

        /// <summary>
        /// Appends a crash entry to <see cref="CrashLogPath"/>. Swallows its own
        /// errors — there is nothing useful to do if even this fails.
        /// </summary>
        private static void WriteCrashLog(string title, Exception ex)
        {
            try
            {
                var path = CrashLogPath;
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                File.AppendAllText(path,
                    $"--- {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}Z  {title}\n" +
                    $"OS: {Environment.OSVersion}\n{ex}\n\n");
            }
            catch { /* ignore */ }
        }
    }
}
