# Screenshots for SEAGit's help on SoftwarePlus.ai (site/seagit/help/images).
#
# Run it with Windows PowerShell 5.1 (powershell.exe): under PowerShell 7 the message boxes' buttons show as panes with
# no Invoke pattern (the Win32 client-side UI Automation proxies are not loaded). Saved with a BOM for 5.1.
#
# Rules (Ron's own desktop): the app is launched here, its windows are found with EnumWindows and photographed with
# PrintWindow (PW_RENDERFULLCONTENT) — never a screen grab — and it is driven only through UI Automation patterns
# (Invoke, Value, SelectionItem, ExpandCollapse): no SendKeys, no mouse. File dialogs are never automated: the list of
# folders is written to %LocalAppData%\SEAGit\repos.json, which is backed up first and restored at the end (deleted if
# there was none). Nothing is published to GitHub: the throwaway folders under -Work have a GitHub address, but
# `url.<local bare repo>.pushInsteadOf` sends their pushes to local bare repositories (see -Work's layout below).
#
# -Work (or -FolderRoot) holds folders\my-cool-website (prepared, linked, empty bare remote), folders\class-notes (not prepared),
# folders\big-video (prepared, linked, a 110 MB file) and folders\old-project (prepared, linked, bare remote that
# already has a README commit), made by hand before this runs.
param(
    [string]$Exe = "",
    [Parameter(Mandatory = $true)][string]$Work,
    [Parameter(Mandatory = $true)][string]$OutDir,
    # Where the four throwaway folders are (their path shows in the pictures, so give it plain names), and their names:
    # prepared + linked + empty remote; not prepared; prepared + linked with a 110 MB file; linked to a remote with a README.
    [string]$FolderRoot = "",
    [string]$Site = 'my-cool-website',
    [string]$New = 'class-notes',
    [string]$Big = 'big-video',
    [string]$Old = 'old-project',
    # Only the Help menu, Help for this screen and About (the folders may already have been published).
    [switch]$HelpOnly,
    # Only "Your name on GitHub" as it opens during a first publish (name-on-github.png). SEAGit is started with the
    # computer's Git settings hidden and no saved name, so it asks; $Site must be prepared, linked and have files. The
    # name box is set to "Pat Smith" for the picture, then Cancel is pressed, so nothing is saved or sent.
    [switch]$NameOnly
)

if (-not $FolderRoot) { $FolderRoot = Join-Path $Work 'folders' }
if (-not $Exe) { $Exe = Join-Path $PSScriptRoot '..\manual-test\SEAGit.exe' }
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName UIAutomationClient, UIAutomationTypes, System.Drawing

Add-Type @"
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
public static class SW {
    [DllImport("user32.dll")] public static extern bool PrintWindow(IntPtr h, IntPtr hdc, uint f);
    [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr h, out RECT r);
    [DllImport("user32.dll")] public static extern bool IsWindowVisible(IntPtr h);
    [DllImport("user32.dll")] public static extern IntPtr SetProcessDpiAwarenessContext(IntPtr v);
    [DllImport("dwmapi.dll")] public static extern int DwmGetWindowAttribute(IntPtr h, int a, out RECT r, int size);
    [DllImport("user32.dll")] static extern bool EnumWindows(EnumProc f, IntPtr l);
    [DllImport("user32.dll")] static extern uint GetWindowThreadProcessId(IntPtr h, out uint pid);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] static extern int GetWindowText(IntPtr h, StringBuilder s, int n);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] static extern int GetClassName(IntPtr h, StringBuilder s, int n);
    delegate bool EnumProc(IntPtr h, IntPtr l);
    [StructLayout(LayoutKind.Sequential)] public struct RECT { public int Left, Top, Right, Bottom; }

    /// Visible top-level windows of a process: "hwnd|class|title".
    public static List<string> Windows(uint pid) {
        var list = new List<string>();
        EnumWindows((h, l) => {
            uint p; GetWindowThreadProcessId(h, out p);
            if (p != pid || !IsWindowVisible(h)) return true;
            var t = new StringBuilder(512); GetWindowText(h, t, 512);
            var c = new StringBuilder(256); GetClassName(h, c, 256);
            list.Add(h.ToInt64() + "|" + c + "|" + t);
            return true;
        }, IntPtr.Zero);
        return list;
    }
}
"@
# Message boxes: their Win32 buttons show in UI Automation as panes with no patterns, so a button is pressed through its
# accessibility default action (MSAA accDoDefaultAction, what UIA's LegacyIAccessible pattern calls) — no keys, no mouse.
Add-Type -ReferencedAssemblies Accessibility @"
using System;
using System.Runtime.InteropServices;
public static class MsaaButton {
    [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr h);
    [DllImport("user32.dll")] static extern bool EnumChildWindows(IntPtr parent, EnumProc f, IntPtr l);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] static extern int GetWindowText(IntPtr h, System.Text.StringBuilder s, int n);
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] static extern int GetClassName(IntPtr h, System.Text.StringBuilder s, int n);
    [DllImport("oleacc.dll")] static extern int AccessibleObjectFromWindow(IntPtr h, uint id, ref Guid iid, [MarshalAs(UnmanagedType.IUnknown)] out object o);
    delegate bool EnumProc(IntPtr h, IntPtr l);
    /// Presses the button (Win32 or WinForms) whose text is the given text, with or without its access-key "&".
    public static bool Press(IntPtr dialog, string text) {
        IntPtr found = IntPtr.Zero;
        EnumChildWindows(dialog, (h, l) => {
            var c = new System.Text.StringBuilder(256); GetClassName(h, c, 256);
            if (c.ToString().IndexOf("button", StringComparison.OrdinalIgnoreCase) < 0) return true;
            var t = new System.Text.StringBuilder(256); GetWindowText(h, t, 256);
            if (t.ToString().Replace("&", "") == text) { found = h; return false; }
            return true;
        }, IntPtr.Zero);
        if (found == IntPtr.Zero) return false;
        Guid iid = typeof(Accessibility.IAccessible).GUID;
        object o;
        if (AccessibleObjectFromWindow(found, 0xFFFFFFFC, ref iid, out o) != 0) return false;
        ((Accessibility.IAccessible)o).accDoDefaultAction(0);
        return true;
    }
}
"@

# Physical pixels everywhere (SEAGit is DPI aware; an unaware caller gets scaled rectangles).
[SW]::SetProcessDpiAwarenessContext([IntPtr]-4) | Out-Null

$UIA = [System.Windows.Automation.AutomationElement]
$CT = [System.Windows.Automation.ControlType]
$Scope = [System.Windows.Automation.TreeScope]

function Get-Windows($procId) {
    foreach ($w in [SW]::Windows([uint32]$procId)) {
        $p = $w.Split('|', 3)
        [pscustomobject]@{ Handle = [IntPtr][long]$p[0]; Class = $p[1]; Title = $p[2] }
    }
}

function Wait-Window($procId, [scriptblock]$Match, [int]$TimeoutMs = 20000) {
    $sw = [Diagnostics.Stopwatch]::StartNew()
    while ($sw.ElapsedMilliseconds -lt $TimeoutMs) {
        $w = Get-Windows $procId | Where-Object $Match | Select-Object -First 1
        if ($w) { Start-Sleep -Milliseconds 700; return $w }
        Start-Sleep -Milliseconds 200
    }
    throw "window not found: $Match  (open: $((Get-Windows $procId | ForEach-Object Title) -join ' / '))"
}

function Wait-MessageBox($procId) { Wait-Window $procId { $_.Class -eq '#32770' -and $_.Title -eq 'SEAGit' } }

function Wait-Gone($procId, [IntPtr]$h, [int]$TimeoutMs = 10000) {
    $sw = [Diagnostics.Stopwatch]::StartNew()
    while ($sw.ElapsedMilliseconds -lt $TimeoutMs) {
        if (-not (Get-Windows $procId | Where-Object { $_.Handle -eq $h })) { Start-Sleep -Milliseconds 300; return }
        Start-Sleep -Milliseconds 150
    }
    throw "window did not close"
}

# The window's bitmap, cropped to its visible frame (GetWindowRect includes the invisible resize borders).
function Get-Bitmap([IntPtr]$h) {
    $r = New-Object SW+RECT; [SW]::GetWindowRect($h, [ref]$r) | Out-Null
    $f = New-Object SW+RECT; [SW]::DwmGetWindowAttribute($h, 9, [ref]$f, 16) | Out-Null
    $full = New-Object System.Drawing.Bitmap ($r.Right - $r.Left), ($r.Bottom - $r.Top)
    $g = [System.Drawing.Graphics]::FromImage($full)
    $hdc = $g.GetHdc(); [SW]::PrintWindow($h, $hdc, 2) | Out-Null; $g.ReleaseHdc($hdc); $g.Dispose()
    $crop = New-Object System.Drawing.Rectangle ($f.Left - $r.Left), ($f.Top - $r.Top), ($f.Right - $f.Left), ($f.Bottom - $f.Top)
    $bmp = $full.Clone($crop, $full.PixelFormat); $full.Dispose()
    [pscustomobject]@{ Bitmap = $bmp; Left = $f.Left; Top = $f.Top }
}

function Save-Shot([IntPtr]$h, [string]$name) {
    $b = Get-Bitmap $h
    $path = Join-Path $OutDir $name
    $b.Bitmap.Save($path, [System.Drawing.Imaging.ImageFormat]::Png)
    Write-Output "saved $name ($($b.Bitmap.Width) x $($b.Bitmap.Height))"
    $b.Bitmap.Dispose()
}

function Get-Element([IntPtr]$h) { $UIA::FromHandle($h) }

function Find-Element($root, [string]$name, $type) {
    $c = New-Object System.Windows.Automation.AndCondition(
        (New-Object System.Windows.Automation.PropertyCondition($UIA::NameProperty, $name)),
        (New-Object System.Windows.Automation.PropertyCondition($UIA::ControlTypeProperty, $type)))
    for ($i = 0; $i -lt 25; $i++) {
        $e = $root.FindFirst($Scope::Descendants, $c)
        if ($e) { return $e }
        # Message box buttons can keep their access-key ampersand ("&Yes") in the name.
        # (In a message box the buttons are reported as panes.)
        foreach ($x in $root.FindAll($Scope::Descendants, [System.Windows.Automation.Condition]::TrueCondition)) {
            if ($x.Current.Name.Replace('&', '') -eq $name -and
                ($x.Current.ControlType -eq $type -or $x.Current.ControlType -eq $CT::Pane)) { return $x }
        }
        Start-Sleep -Milliseconds 200
    }
    $seen = $root.FindAll($Scope::Descendants, [System.Windows.Automation.Condition]::TrueCondition) | ForEach-Object { "$($_.Current.ControlType.ProgrammaticName)='$($_.Current.Name)'" }
    throw "not found: $name; seen: $($seen -join ', ')"
}

# Invoke on a button that opens a modal window does not return until that window closes, so it runs on its own thread.
$script:background = @()
function Invoke-Async($element) {
    $ps = [powershell]::Create()
    [void]$ps.AddScript({ param($e) $e.GetCurrentPattern([System.Windows.Automation.InvokePattern]::Pattern).Invoke() }).AddArgument($element)
    [void]$ps.BeginInvoke()
    $script:background += $ps
}

function Press($root, [string]$name) { Invoke-Async (Find-Element $root $name $CT::Button) }

# Presses a button in a window (message box or form) and waits for the window to close.
function Answer-Box($procId, $box, [string]$button) {
    # A button's default action (BM_CLICK) is ignored unless its dialog is the foreground window.
    for ($try = 0; $try -lt 4; $try++) {
        [MsaaButton]::SetForegroundWindow($box.Handle) | Out-Null
        Start-Sleep -Milliseconds 300
        if (-not [MsaaButton]::Press($box.Handle, $button)) { throw "message box button not found: $button" }
        try { Wait-Gone $procId $box.Handle 3000; return } catch { }
    }
    throw "message box did not close: $button"
}

function Select-Folder($main, [string]$folder) {
    $items = $main.FindAll($Scope::Descendants, (New-Object System.Windows.Automation.PropertyCondition($UIA::ControlTypeProperty, $CT::ListItem)))
    foreach ($it in $items) {
        if ($it.Current.Name -like "$folder*") {
            $it.GetCurrentPattern([System.Windows.Automation.SelectionItemPattern]::Pattern).Select()
            Start-Sleep -Milliseconds 400
            return
        }
    }
    throw "list item not found: $folder"
}

# Opens the Help menu; a menu drop-down closes at once unless its window is in front, so it is tried a few times.
function Open-HelpMenu($app, $help) {
    $id = $app.Process.Id
    for ($try = 0; $try -lt 5; $try++) {
        $drop = Get-Windows $id | Where-Object { $_.Handle -ne $app.Window.Handle -and $_.Class -like 'WindowsForms*' } | Select-Object -First 1
        if ($drop) { return $drop }
        [MsaaButton]::SetForegroundWindow($app.Window.Handle) | Out-Null
        Start-Sleep -Milliseconds 400
        $help.GetCurrentPattern([System.Windows.Automation.ExpandCollapsePattern]::Pattern).Expand()
        Start-Sleep -Milliseconds 900
    }
    return $null
}

function Start-SEAGit {
    $p = Start-Process -FilePath $Exe -PassThru
    $w = Wait-Window $p.Id { $_.Title -like 'SEAGit — Super-Easy*' }
    Start-Sleep -Milliseconds 1500
    [pscustomobject]@{ Process = $p; Window = $w; Root = (Get-Element $w.Handle) }
}

function Stop-SEAGit($app) {
    try { $app.Root.GetCurrentPattern([System.Windows.Automation.WindowPattern]::Pattern).Close() } catch { }
    if (-not $app.Process.WaitForExit(5000)) { $app.Process.Kill() }
}

New-Item -ItemType Directory -Force $OutDir | Out-Null
$listDir = Join-Path $env:LOCALAPPDATA 'SEAGit'
$list = Join-Path $listDir 'repos.json'
$backup = Join-Path $listDir 'repos.json.help-shots-backup'
$hadList = Test-Path $list
if ($hadList) { Copy-Item $list $backup -Force }
# The saved name on GitHub (identity.json) is kept aside too, and put back at the end.
$identity = Join-Path $listDir 'identity.json'
$identityBackup = Join-Path $listDir 'identity.json.help-shots-backup'
$hadIdentity = Test-Path $identity
if ($hadIdentity) { Move-Item $identity $identityBackup -Force }
$app = $null

try {
  if ($NameOnly) {
    # Git's own settings hidden for this SEAGit only (it passes its environment to git.exe): no name anywhere, so it asks.
    $emptyConfig = Join-Path $Work 'empty.gitconfig'
    Set-Content -Path $emptyConfig -Value '' -Encoding ascii
    $savedEnv = @{ GIT_CONFIG_GLOBAL = $env:GIT_CONFIG_GLOBAL; GIT_CONFIG_NOSYSTEM = $env:GIT_CONFIG_NOSYSTEM }
    $env:GIT_CONFIG_GLOBAL = $emptyConfig; $env:GIT_CONFIG_NOSYSTEM = '1'
    try {
        New-Item -ItemType Directory -Force $listDir | Out-Null
        ConvertTo-Json @([ordered]@{ Name = $Site; LocalPath = (Join-Path $FolderRoot $Site); LastPublished = '0001-01-01T00:00:00' }) |
            Set-Content -Path $list -Encoding utf8
        $app = Start-SEAGit
    } finally {
        $env:GIT_CONFIG_GLOBAL = $savedEnv.GIT_CONFIG_GLOBAL; $env:GIT_CONFIG_NOSYSTEM = $savedEnv.GIT_CONFIG_NOSYSTEM
    }
    $id = $app.Process.Id; $main = $app.Root
    Select-Folder $main $Site
    Press $main 'Publish Selected'
    # Asked before "N files … will be published … Publish now?"
    $nw = Wait-Window $id { $_.Title -eq 'SEAGit — Your name on GitHub' }
    $ne = Get-Element $nw.Handle
    (Find-Element $ne 'Name to show on GitHub' $CT::Edit).GetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern).SetValue('Pat Smith')
    Start-Sleep -Milliseconds 500
    Save-Shot $nw.Handle 'name-on-github.png'
    Answer-Box $id $nw 'Cancel'
    Start-Sleep -Milliseconds 500
    return
  }

    # 1. The empty main window: no list at all.
    if (Test-Path $list) { Remove-Item $list }
    $app = Start-SEAGit
    Save-Shot $app.Window.Handle 'main-empty.png'
    Stop-SEAGit $app; $app = $null

    # 2. A list of four throwaway folders.
    $folders = $Site, $New, $Big, $Old | ForEach-Object {
        [ordered]@{ Name = $_; LocalPath = (Join-Path $FolderRoot $_); LastPublished = '0001-01-01T00:00:00' }
    }
    New-Item -ItemType Directory -Force $listDir | Out-Null
    ConvertTo-Json @($folders) | Set-Content -Path $list -Encoding utf8
    $app = Start-SEAGit
    $id = $app.Process.Id; $main = $app.Root

    if (-not $HelpOnly) {
        # First publish of a prepared, linked folder: the count question, then the result.
        Select-Folder $main $Site
        Press $main 'Publish Selected'
        $box = Wait-MessageBox $id; Save-Shot $box.Handle 'publish-first-question.png'
        Answer-Box $id $box 'Yes'
        $box = Wait-MessageBox $id; Save-Shot $box.Handle 'publish-done.png'
        Answer-Box $id $box 'OK'

        # Publish again: nothing has changed.
        Press $main 'Publish Selected'
        $box = Wait-MessageBox $id; Save-Shot $box.Handle 'publish-nothing.png'
        Answer-Box $id $box 'OK'

        # The main window with its folders and the Activity Log of that publish.
        Save-Shot $app.Window.Handle 'main-folders.png'

        # A folder SEAGit has not used: the prepare question, then the GitHub address window (cancelled).
        Select-Folder $main $New
        Press $main 'Publish Selected'
        $box = Wait-MessageBox $id; Save-Shot $box.Handle 'prepare-question.png'
        Answer-Box $id $box 'Yes'
        $addr = Wait-Window $id { $_.Title -eq 'SEAGit — Link to GitHub' }
        Save-Shot $addr.Handle 'address-window.png'
        $ae = Get-Element $addr.Handle
        $edit = Find-Element $ae 'GitHub repository address' $CT::Edit
        $edit.GetCurrentPattern([System.Windows.Automation.ValuePattern]::Pattern).SetValue('github.com/your-name')
        Press $ae 'Link and Publish'
        Start-Sleep -Milliseconds 900
        Save-Shot $addr.Handle 'address-window-problem.png'
        Start-Sleep -Milliseconds 3000
        Save-Shot $addr.Handle 'address-window-problem-later.png'
        Answer-Box $id $addr 'Cancel'

        # A file over 100 MB stops the publish before anything is saved.
        Select-Folder $main $Big
        Press $main 'Publish Selected'
        $box = Wait-MessageBox $id; Save-Shot $box.Handle 'publish-too-big.png'
        Answer-Box $id $box 'OK'

        # A GitHub repository that already has a README: refused.
        Select-Folder $main $Old
        Press $main 'Publish Selected'
        $box = Wait-MessageBox $id; Answer-Box $id $box 'Yes'
        $box = Wait-MessageBox $id; Save-Shot $box.Handle 'publish-refused.png'
        Answer-Box $id $box 'OK'

        Select-Folder $main $Site
    }

    # The Help menu, open: the main window with the drop-down drawn over it.
    $help = Find-Element $main 'Help' $CT::MenuItem
    $drop = Open-HelpMenu $app $help
    if ($drop) {
        $m = Get-Bitmap $app.Window.Handle
        $d = Get-Bitmap $drop.Handle
        $right = [Math]::Max($m.Left + $m.Bitmap.Width, $d.Left + $d.Bitmap.Width)
        $bottom = [Math]::Max($m.Top + $m.Bitmap.Height, $d.Top + $d.Bitmap.Height)
        $canvas = New-Object System.Drawing.Bitmap ($right - $m.Left), ($bottom - $m.Top)
        $g = [System.Drawing.Graphics]::FromImage($canvas)
        $g.DrawImage($m.Bitmap, 0, 0); $g.DrawImage($d.Bitmap, ($d.Left - $m.Left), ($d.Top - $m.Top)); $g.Dispose()
        $canvas.Save((Join-Path $OutDir 'help-menu.png'), [System.Drawing.Imaging.ImageFormat]::Png)
        Write-Output "saved help-menu.png ($($canvas.Width) x $($canvas.Height))"
        $canvas.Dispose(); $m.Bitmap.Dispose(); $d.Bitmap.Dispose()
    } else { Write-Output "Help menu drop-down not found" }

    # Help ▸ Help for this screen (F1).
    Open-HelpMenu $app $help | Out-Null
    Invoke-Async (Find-Element $main 'Help for this screen' $CT::MenuItem)
    $hw = Wait-Window $id { $_.Title -eq 'SEAGit — Help for this screen' }
    Save-Shot $hw.Handle 'help-for-this-screen.png'
    Answer-Box $id $hw 'Close'

    # Help ▸ About SEAGit…
    Open-HelpMenu $app $help | Out-Null
    Invoke-Async (Find-Element $main 'About SEAGit…' $CT::MenuItem)
    $ab = Wait-Window $id { $_.Title -eq 'About SEAGit' }
    Save-Shot $ab.Handle 'about.png'
    Answer-Box $id $ab 'Close'
}
finally {
    if ($app) { Stop-SEAGit $app }
    foreach ($ps in $script:background) { try { $ps.Stop(); $ps.Dispose() } catch { } }
    if ($hadList) { Move-Item $backup $list -Force; Write-Output "restored the original repos.json" }
    else { Remove-Item $list, "$list.tmp" -ErrorAction SilentlyContinue; Write-Output "removed the temporary repos.json (there was none before)" }
    Remove-Item $identity, "$identity.tmp" -ErrorAction SilentlyContinue
    if ($hadIdentity) { Move-Item $identityBackup $identity -Force; Write-Output "restored the original identity.json" }
}
