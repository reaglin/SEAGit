using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;
using Windows.Services.Store;

namespace SEAGit.Services
{
    /// <summary>
    /// Help ▸ Check for a new version (Ron, 2026-10-30, for every Store program; copied from Accessibility+'s StoreUpdates).
    /// Asks the Microsoft Store whether a newer version of SEAGit is published; if so, asks the person, then lets the Store
    /// download and install it (Windows shows its own progress). A copy not installed from the Store (a hand-test or
    /// installer build) cannot ask, and says so, offering the Store page instead.
    /// </summary>
    public static class StoreUpdates
    {
        public const string ProgramName = "SEAGit";
        public const string StorePage = "https://apps.microsoft.com/detail/9P7Z72K774JN";
        private const string Title = ProgramName + " — Check for a new version";

        /// <summary>The version running now, as people read it (1.0.5) — the same number Help ▸ About shows.</summary>
        public static string CurrentVersion
        {
            get
            {
                var v = typeof(StoreUpdates).Assembly.GetName().Version;
                return v == null ? "" : $"{v.Major}.{v.Minor}.{v.Build}";
            }
        }

        public static async Task CheckAsync(Form owner)
        {
            if (!IsFromStore())
            {
                if (MessageBox.Show(owner,
                        $"This copy of {ProgramName} ({CurrentVersion}) was not installed from the Microsoft Store, so it cannot check for a " +
                        "new version itself.\n\nOpen the program's page on the Microsoft Store to see the latest version?",
                        Title, MessageBoxButtons.YesNo, MessageBoxIcon.Information) == DialogResult.Yes)
                    OpenStorePage(owner);
                return;
            }

            IReadOnlyList<StorePackageUpdate> updates;
            StoreContext store;
            try
            {
                store = StoreContext.GetDefault();
                // A desktop program must tell the Store which window its own dialogs belong to.
                WinRT.Interop.InitializeWithWindow.Initialize(store, owner.Handle);
                updates = await store.GetAppAndOptionalStorePackageUpdatesAsync();
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Store check failed: " + ex);
                if (MessageBox.Show(owner,
                        "The Microsoft Store could not be asked about a new version. Check that this computer is connected to the internet.\n\n" +
                        $"Open {ProgramName}'s page on the Microsoft Store instead?",
                        Title, MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                    OpenStorePage(owner);
                return;
            }

            if (updates.Count == 0)
            {
                MessageBox.Show(owner, $"You have the latest version of {ProgramName} ({CurrentVersion}).", Title,
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            if (MessageBox.Show(owner,
                    $"A new version of {ProgramName} is on the Microsoft Store (you have {CurrentVersion}).\n\n" +
                    $"Download and install it now? Your list of folders is kept. Windows closes {ProgramName} to install it; " +
                    "open it again from the Start menu. (No: the Store installs it later on its own.)",
                    Title, MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;

            try
            {
                var result = await store.RequestDownloadAndInstallStorePackageUpdatesAsync(updates);
                string message = result.OverallState switch
                {
                    StorePackageUpdateState.Completed => $"The new version is installed. If {ProgramName} is still open, close it and open it again from the Start menu.",
                    StorePackageUpdateState.Canceled => "The update was cancelled. You can check again at any time, or the Store will install it later.",
                    _ => $"The update did not finish. The Microsoft Store will try again later, or open " +
                         $"{ProgramName}'s page on the Store and press Update.",
                };
                MessageBox.Show(owner, message, Title, MessageBoxButtons.OK,
                    result.OverallState == StorePackageUpdateState.Completed ? MessageBoxIcon.Information : MessageBoxIcon.Warning);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("Store update failed: " + ex);
                MessageBox.Show(owner, $"The update could not be started. Open {ProgramName}'s page on the Microsoft Store " +
                    "and press Update instead.", Title, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private static void OpenStorePage(Form owner)
        {
            try { Process.Start(new ProcessStartInfo(StorePage) { UseShellExecute = true }); }
            catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or InvalidOperationException)
            {
                MessageBox.Show(owner, $"Windows could not open the page.\n\n{StorePage}", Title,
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        /// <summary>True when the program runs from its Store package (it has a package identity).</summary>
        public static bool IsFromStore()
        {
            int length = 0;
            // APPMODEL_ERROR_NO_PACKAGE (15700) means it was not installed as a package.
            return GetCurrentPackageFullName(ref length, null) != 15700;
        }

        [DllImport("kernel32.dll", CharSet = CharSet.Unicode)]
        private static extern int GetCurrentPackageFullName(ref int packageFullNameLength, char[]? packageFullName);
    }
}
