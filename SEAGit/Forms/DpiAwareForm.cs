using System.Windows.Forms;

namespace SEAGit.Forms
{
    /// <summary>
    /// Base form that applies DPI scaling for SEAGit's hand-coded forms.
    ///
    /// SEAGit lays out its windows with 96-DPI pixel coordinates. WinForms'
    /// <see cref="AutoScaleMode.Dpi"/> only scales controls that exist when the
    /// auto-scale baseline is established, so AutoScaleMode / AutoScaleDimensions
    /// must be set *after* every child control has been added. The designer-
    /// generated values (Font / 7×15) are overridden here so the 96→DeviceDpi
    /// ratio is applied to the whole control tree in <see cref="OnLoad"/>.
    ///
    /// Any form that overrides <see cref="OnLoad"/> MUST call <c>base.OnLoad(e)</c>,
    /// otherwise scaling never runs. Matches the pattern documented in the sibling
    /// CIATLE repository's DISPLAY_FIX.md.
    /// </summary>
    public class DpiAwareForm : Form
    {
        protected override void OnLoad(EventArgs e)
        {
            AutoScaleDimensions = new SizeF(96f, 96f);
            AutoScaleMode       = AutoScaleMode.Dpi;
            PerformAutoScale();
            base.OnLoad(e);
        }
    }
}
