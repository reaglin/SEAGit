using System.Drawing;
using System.Drawing.Drawing2D;
using System.Reflection;

namespace SEAGit
{
    /// <summary>
    /// Help ▸ About SEAGit: who made it, what it is for, and the version.
    /// Ron, 2026-09-25: "Created by Dr. Ron Eaglin" with his picture, in every program's About.
    /// </summary>
    public class AboutForm : Form
    {
        private const string Description =
            "SEAGit takes a folder on your computer and puts it on GitHub, where it is kept safe and can be shared. " +
            "There is no branching, no staging and no command line: add a folder, select it, and press Publish Selected. " +
            "Use it to keep copies of your work, or to publish a simple website with GitHub Pages. " +
            "All you need is a free GitHub account: Git comes built in.";

        public AboutForm()
        {
            Text = "About SEAGit";
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowInTaskbar = false;
            AutoScaleMode = AutoScaleMode.Font;
            AutoSize = true;
            AutoSizeMode = AutoSizeMode.GrowAndShrink;
            Padding = new Padding(16);

            var layout = new TableLayoutPanel
            {
                ColumnCount = 2,
                RowCount = 4,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Dock = DockStyle.Fill,
            };
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));

            var picture = new PictureBox
            {
                Size = new Size(96, 96),
                SizeMode = PictureBoxSizeMode.Zoom,
                Margin = new Padding(0, 0, 16, 12),
                AccessibleName = "Picture of Dr. Ron Eaglin",
            };
            string picturePath = Path.Combine(AppContext.BaseDirectory, "Images", "dr-ron-eaglin.jpg");
            if (File.Exists(picturePath))
            {
                // Loaded from a copy so the file is not held open.
                using var stream = File.OpenRead(picturePath);
                using var loaded = Image.FromStream(stream);
                picture.Image = new Bitmap(loaded);
            }
            picture.Region = RoundedRegion(picture.Size, 14);
            FormClosed += (_, _) => picture.Image?.Dispose();

            var heading = new Label
            {
                Text = "Created by Dr. Ron Eaglin",
                AutoSize = true,
                Font = new Font("Segoe UI", 14F, FontStyle.Bold),
                Margin = new Padding(0, 16, 0, 4),
            };
            var product = new Label
            {
                Text = "SEAGit — Super-Easy and Accessible Git",
                AutoSize = true,
                Margin = new Padding(0, 0, 0, 2),
            };
            var version = new Label
            {
                Text = "Version " + VersionText(),
                AutoSize = true,
                ForeColor = SystemColors.GrayText,
                Margin = new Padding(0),
            };

            var headingStack = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.TopDown,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                WrapContents = false,
                Margin = new Padding(0),
            };
            headingStack.Controls.Add(heading);
            headingStack.Controls.Add(product);
            headingStack.Controls.Add(version);

            var description = new Label
            {
                Text = Description,
                AutoSize = true,
                MaximumSize = new Size(440, 0),
                Margin = new Padding(0, 0, 0, 16),
            };

            // Links (portfolio rules, 2026-09-30 and 2026-10-30): the program's page, its Store page, all the programs.
            var links = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.TopDown,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                WrapContents = false,
                Margin = new Padding(0, 0, 0, 16),
            };
            links.Controls.Add(MakeLink("More about SEAGit, with help and examples: ",
                "softwareplus.ai/seagit", "https://softwareplus.ai/seagit/"));
            links.Controls.Add(MakeLink("SEAGit on the Microsoft Store (updates, ratings and reviews): ",
                "apps.microsoft.com/detail/9P7Z72K774JN", "https://apps.microsoft.com/detail/9P7Z72K774JN"));
            links.Controls.Add(MakeLink("All of Dr. Ron Eaglin's programs: ",
                "softwareplus.ai", "https://softwareplus.ai/#programs"));

            var close = new Button
            {
                Text = "Close",
                DialogResult = DialogResult.OK,
                AutoSize = true,
                MinimumSize = new Size(90, 30),
                Anchor = AnchorStyles.Right,
                Margin = new Padding(0),
            };

            layout.Controls.Add(picture, 0, 0);
            layout.Controls.Add(headingStack, 1, 0);
            layout.Controls.Add(description, 0, 1);
            layout.SetColumnSpan(description, 2);
            layout.Controls.Add(links, 0, 2);
            layout.SetColumnSpan(links, 2);
            layout.Controls.Add(close, 0, 3);
            layout.SetColumnSpan(close, 2);

            Controls.Add(layout);
            AcceptButton = close;
            CancelButton = close;
        }

        /// <summary>The version the program was built as, e.g. 1.0.0.</summary>
        private static string VersionText()
        {
            Version? v = Assembly.GetExecutingAssembly().GetName().Version;
            return v == null ? "unknown" : $"{v.Major}.{v.Minor}.{v.Build}";
        }

        /// <summary>
        /// One line of text ending in a link. Tab reaches it; Enter or Space opens the address in
        /// the person's web browser. SEAGit itself makes no connection.
        /// </summary>
        private LinkLabel MakeLink(string lead, string linkText, string url)
        {
            var label = new LinkLabel
            {
                Text = lead + linkText,
                AutoSize = true,
                MaximumSize = new Size(440, 0),
                Margin = new Padding(0, 0, 0, 4),
                TabStop = true,
                AccessibleName = lead + linkText,
                AccessibleDescription = "Opens " + url + " in your web browser",
            };
            label.LinkArea = new LinkArea(lead.Length, linkText.Length);
            label.Links[0].LinkData = url;
            label.LinkClicked += (_, e) =>
            {
                string address = (string)e.Link!.LinkData!;
                try
                {
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(address) { UseShellExecute = true });
                    e.Link.Visited = true;
                }
                catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or InvalidOperationException)
                {
                    MessageBox.Show(this, "Windows could not open a web browser. The address is " + address,
                        "About SEAGit", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            };
            return label;
        }

        private static Region RoundedRegion(Size size, int radius)
        {
            int d = radius * 2;
            using var path = new GraphicsPath();
            path.AddArc(0, 0, d, d, 180, 90);
            path.AddArc(size.Width - d, 0, d, d, 270, 90);
            path.AddArc(size.Width - d, size.Height - d, d, d, 0, 90);
            path.AddArc(0, size.Height - d, d, d, 90, 90);
            path.CloseFigure();
            return new Region(path);
        }
    }
}
