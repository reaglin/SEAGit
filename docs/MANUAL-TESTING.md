# SEAGit — manual testing

Hand-tests Ron (or another tester) runs before a Store release. The plan these feed is
[`DEVELOPMENT-PLAN.md`](DEVELOPMENT-PLAN.md).

## The hand-test build

From the repository root (`SeaGit\SEAGit\`):

```powershell
dotnet publish SEAGit\SEAGit.csproj -c Release -r win-x64 --self-contained false -o manual-test
```

It makes `manual-test\SEAGit.exe` (gitignored), with the built-in Git under `manual-test\MinGit\`. It needs the
.NET 8 Desktop Runtime, which this computer has. It is not the Store copy, so Help ▸ Check for a new version says it
cannot check (that is expected).

The automatic tests (Git steps against a temporary local "GitHub", no network) run with:

```powershell
dotnet test SEAGit.Tests -c Release
```

**You need:** a GitHub account, and for part B two **new, empty** GitHub repositories (+ ▸ New repository, leave
"Add a README file", .gitignore and license unticked), e.g. `seagit-test-1` and `seagit-test-2`, plus one GitHub
repository that **has** a README (tick "Add a README file"), e.g. `seagit-test-readme`. Delete them afterwards.

---

## Round 2026-11 — usability (Ron, 2026-10-31)

Build: `manual-test\SEAGit.exe` (made 2026-10-31, version 1.0.6). Close any copy that is open first.
Record each step as ✅ / ❌ with a note; ❌ and notes go back into docs/DEVELOPMENT-PLAN.md.

### A. First start, as a new user

A1. Start it. Does the window say what SEAGit is for and what to do first (the three numbered steps at the top)?
    The Activity Log should say the list is empty and to press + Add Local Folder… to start.
A2. **Add a folder:** make a folder `Documents\seagit-test` with an `index.html` in it. Press **+ Add Local Folder…**
    and choose it — expected: the question "SEAGit has not used this folder before. Prepare it for publishing? …";
    Yes adds it to **Your folders** as "seagit-test — never published — C:\…".
A3. **Publish it the first time:** select it, press **Publish Selected** — expected: the **Link to GitHub** window with
    the steps for making an empty GitHub repository. Paste the address of `seagit-test-1` → **Link and Publish** →
    "1 file (… bytes) in "seagit-test" will be published to the GitHub repository …  Publish now?" → Yes → the status
    line says "Publishing "seagit-test"… a GitHub sign-in window may open in your browser." and the window keeps
    responding (move it, scroll the log). Sign in in the browser if asked. Expected: "Published 1 file to GitHub."
    and the file is on github.com.
A4. **Publish a change:** edit `index.html`, add `about.html`, press Publish Selected — expected: no questions,
    "Published 2 files to GitHub." Press it again — "Nothing to publish: everything in "seagit-test" is already on GitHub."
A5. At each step, could you tell what to do next without knowing Git?

### B. This round's fixes

B1. *(1, unpublished changes)* Turn off Wi-Fi. Edit `index.html`, press Publish Selected — expected: "The folder was
    not published. SEAGit could not reach GitHub. …". Turn Wi-Fi on, press Publish Selected **without changing
    anything** — expected: "Published the changes saved earlier that had not reached GitHub yet (1 file)." and the
    change is on github.com.
B2. *(2, no Git name)* On a computer (or Windows account) that has never had Git set up, publish a new folder —
    expected: it works (no "Author identity unknown"). On github.com the commit shows your Windows user name.
B3. *(3, responsive window / sign-in)* During A3, the window can be moved and the log scrolled while the browser
    sign-in is open; the buttons are greyed and the status line says they come back when SEAGit has finished. To try a
    failed sign-in: in Windows Credential Manager remove the `git:https://github.com` entry, publish, and close Git's
    "Connect to GitHub" window without signing in — expected: "GitHub sign-in did not finish … Press Publish Selected
    again …", and SEAGit is never stuck waiting on a question nobody can see (note it if it waits more than a minute). Try closing SEAGit during a publish — expected: it asks first.
B4. *(4, plain-words messages)* Every outcome in A and B showed a message box (success too), with Git's own text only
    in the Activity Log. Make a new folder and link it to `seagit-test-readme` (the one with a README) — expected:
    "That GitHub repository already has files that are not in this folder … Make a new, empty GitHub repository …".
    Change GitHub Address… to `https://github.com/<you>/does-not-exist-xyz` and publish — expected: "GitHub could not
    find that GitHub repository, or your GitHub account is not allowed to publish to it …".
B5. *(5, master)* Take an existing local Git folder whose branch is `master` (or make one with Git elsewhere), add it,
    link it to an empty GitHub repository and publish — expected: it publishes, and GitHub shows branch `master`.
B6. *(6, the address window)* In Link to GitHub: press Cancel — expected: nothing published, the log says "Publish
    cancelled: no GitHub repository address was given." Type `www.example.com` and press Link and Publish — expected:
    a red line under the box says why ("That is not a GitHub repository address. It must start with https://github.com/ …")
    and the window stays open. Press **Help (F1)** — help about making an empty GitHub repository.
    **Change GitHub Address…** on `seagit-test`: paste `seagit-test-2`'s address → Save Address → "From now on,
    "seagit-test" publishes to …" → Publish Selected → the whole folder is published to `seagit-test-2`.
B7. *(7, main window)* **Open Folder** (and double-clicking a folder) opens it in File Explorer. **Remove from List** asks
    "Remove "seagit-test" from SEAGit's list? Your files are not deleted …"; Yes removes it and the folder is still on
    disk. Add it again: it keeps its GitHub address. The heading reads **Your folders**.
B8. *(8, dangerous folders)* + Add Local Folder… → choose `C:\` — refused ("… is a whole drive, not a folder …"); choose
    your user folder `C:\Users\<you>` — refused. Put a file over 100 MB (e.g. a video) in `seagit-test` and publish —
    expected: "Nothing was published. GitHub does not accept files over 100 MB …" naming the file; move it out and publish.
B9. *(minors)* Press Publish Selected with nothing selected — "Select a folder in the list first, then press Publish
    Selected." Help ▸ About opens at the right size at 150 % with Dr. Ron's picture rounded (not cut to a corner). Close
    SEAGit, put `{ broken` into `%LOCALAPPDATA%\SEAGit\repos.json`, start it — expected: a message that the list could
    not be read, that your folders are not affected, and where a copy of the old list was kept
    (`repos.unreadable-….json` beside it).
B10. *(usability round)* Hover every button, the list and the log: each has a tooltip. Press **F1** in the main window —
    Help for this screen opens with help about the window and a link to softwareplus.ai/seagit/help/.
B11. *(UX review fixes)* With a folder that has never been published, use Change GitHub Address… first — the window is
    titled "SEAGit — Change GitHub Address" and its button says **Save Address** (nothing is published). Paste an SSH
    address `git@github.com:<you>/seagit-test-1.git` — accepted as the HTTPS address. On a folder already published, edit
    a file on github.com, then change it in the folder and publish — expected: "The GitHub repository has changes that
    are not in this folder … edited on github.com …" (not "make a new repository"). Remove a published folder and add it
    again — its "last published" date comes back. Answer No to "Prepare it for publishing?" — the log says it was not
    added. With an empty list the box says "No folders yet …"; clicking it opens the folder chooser.
B12. *(1.15, long paths and the idle line)* Add a folder with a long place, e.g. `C:\Users\Public\Documents\SEAGit example\My class website`.
    Expected: its row fits the list, the middle of the place replaced by "…" (the folder's own name stays at the end), and
    there is no sideways scrollbar. Rest the mouse on the row — the tooltip shows the full place. Make the window wider and
    narrower — the rows re-fit. Open Folder (and double-click) opens the right folder. With an empty list the bold line
    under the buttons says "Ready. Press + Add Local Folder… to start."; with folders, "Ready. Select a folder, then press
    Publish Selected."; while publishing it says what SEAGit is doing, and "Ready …" comes back when it has finished.
    Also at 150 %: nothing cut off.

### C. Every screen (USABILITY-CHECKLIST.md 1–4)

For each window: **the SEAGit window**, **Link to GitHub / Change GitHub Address**, **Help for this screen**,
**About SEAGit**, and the message boxes of Check for a new version. Hover every control: tooltip? Disabled ones say
why (the status line, while publishing)? F1 opens help about this screen (main window, address window)? Empty list says
what to do (the intro and the log)? A wrong address says why?

### D. Same as every program (checklist 5–6)

D1. SEAGit has no documents to save; closing during a publish asks first (B3).
D2. Help menu, in this order: **Help for this screen (F1)** · **Help on the web (softwareplus.ai/seagit)** ·
    **Check for a new version…** · **About SEAGit…**.
D3. About: version **1.0.6**, and the four links work (SEAGit's page, the Microsoft Store page, all programs, the
    privacy policy).
D4. Check for a new version: the status line says "Asking the Microsoft Store…"; this hand-test copy says it was not
    installed from the Microsoft Store and offers the Store page. Every message box is titled "SEAGit" or "SEAGit — …".

### E. Look (checklist 7)

At 150 % scaling and the smallest window size: the intro, the bottom row of buttons (Open Folder, Change GitHub
Address…, Remove from List), the two-line status line and the Link to GitHub window — anything cut off, hard to read,
or colour-only? The Publish Selected button is now a darker green so its white text is easier to read.
