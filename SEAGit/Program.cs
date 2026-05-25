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
            // Route UI-thread exceptions to Application.ThreadException (below)
            // instead of letting them terminate the process. Without this the
            // mode is "Automatic", and on some configurations a launch-time UI
            // exception becomes a fatal unhandled exception with no dialog —
            // exactly the silent "crashes at launch" the Store cert lab reported.
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);

            // Install crash handlers FIRST so a launch-time exception is logged
            // AND shown rather than terminating the process silently. The cert
            // lab returns a screen capture, not files, so every handler must put
            // the full error (type + message + stack) on screen — a crash-log
            // file alone tells us nothing about a failure we can't reproduce.
            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            {
                var ex = e.ExceptionObject as Exception;
                WriteCrashLog("Unhandled domain exception", ex);
                ShowError("Unhandled error", ex);
            };

            Application.ThreadException += (s, e) =>
            {
                WriteCrashLog("Unhandled UI thread exception", e.Exception);
                ShowError("UI thread error", e.Exception);
            };

            try
            {
                WriteCrashLog("Startup breadcrumb", null, "Main entered");
                ApplicationConfiguration.Initialize();

                WriteCrashLog("Startup breadcrumb", null, "Config initialized; creating MainForm");
                var form = new MainForm();

                WriteCrashLog("Startup breadcrumb", null, "MainForm created; entering message loop");
                Application.Run(form);
            }
            catch (Exception ex)
            {
                WriteCrashLog("SEAGit startup failed", ex);
                ShowError("Startup error", ex);
            }
        }

        /// <summary>
        /// Shows the full exception detail in a message box so it is captured by
        /// the cert lab's screen grab. Truncated to stay on screen; the complete
        /// trace is always in the crash log. Guarded — never throws.
        /// </summary>
        private static void ShowError(string stage, Exception? ex)
        {
            try
            {
                string detail = Describe(stage, ex);
                if (detail.Length > 1800) detail = detail[..1800] + "\n…(truncated; full detail in crash log)";

                MessageBox.Show(
                    "SEAGit could not start because of an unexpected error.\n\n" +
                    detail +
                    "\n\nFull crash log:\n" + CrashLogPath,
                    "SEAGit — Startup Error",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch { /* nothing more we can do if even the dialog fails */ }
        }

        /// <summary>
        /// Appends a crash (or breadcrumb) entry to <see cref="CrashLogPath"/>.
        /// Swallows its own errors — there is nothing useful to do if even this
        /// fails. Pass <paramref name="note"/> for a breadcrumb with no exception.
        /// </summary>
        private static void WriteCrashLog(string title, Exception? ex, string? note = null)
        {
            try
            {
                var path = CrashLogPath;
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                File.AppendAllText(path,
                    $"--- {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss}Z  {title}\n" +
                    Describe(note ?? title, ex) + "\n\n");
            }
            catch { /* ignore */ }
        }

        /// <summary>
        /// Builds a single diagnostic string: stage, OS, exception type, message,
        /// stack, and inner-exception chain. Used for both the log and the dialog.
        /// </summary>
        private static string Describe(string stage, Exception? ex)
        {
            string text = $"Stage: {stage}\nOS: {Environment.OSVersion}  64-bit: {Environment.Is64BitOperatingSystem}";
            for (var e = ex; e != null; e = e.InnerException)
            {
                text += $"\n\n{e.GetType().FullName}: {e.Message}";
                if (!string.IsNullOrEmpty(e.StackTrace))
                    text += "\n" + e.StackTrace;
            }
            return text;
        }
    }
}
