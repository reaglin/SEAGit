# SEAGit — development plan

SEAGit (Super-Easy and Accessible Git) publishes a folder from a person's computer to GitHub, for people new to Git.
WinForms, .NET 8, on the Microsoft Store (Store ID 9P7Z72K774JN, https://apps.microsoft.com/detail/9P7Z72K774JN).
This plan was started on **2026-10-31** with the pre-release UX round for **1.0.6** (portfolio rule: a repo without a
plan starts one when it is next worked on). Earlier work is in the git history and `DEPLOY_TO_WIN_APP_STORE.md`.

---

## How an item is marked

| Mark | Means |
|---|---|
| ⬜ *(or no icon)* | to do — not started or still being built |
| ⚠️ | **action needed** — built and tested, waiting for Ron to verify (the row says what to check); or returned by Ron with a comment; or waiting on an answer from Ron |
| ✅ | done — built, tested **and verified by Ron** |

Claude never marks an item ✅; only Ron's confirmation does. Hand-test steps are in
[`MANUAL-TESTING.md`](MANUAL-TESTING.md), round 2026-11 (the B-step for each row is given).

---

## Phase 1 — the 1.0.6 UX round (2026-10-31)

A pre-release UX review found 2 critical and 6 major problems, plus minors; Ron then asked for a usability pass
over every screen against `USABILITY-CHECKLIST.md` (all ten Store programs, 2026-10-31). Every item was checked in the
code before it was changed (all of the review's claims were right). Tests: `SEAGit.Tests`, 33 green — the Git steps run
with the bundled MinGit against a temporary bare repository standing in for GitHub (no network), with the computer's
own Git settings hidden. Release build: no warnings (the 4 nullable warnings that were there are gone).

**UX review 2026-10-31:** walkthrough (6 tasks) + heuristics + USABILITY-CHECKLIST 1–6 by the ux-reviewer agent, from source: 0 critical, 2 major, 7 minor; both majors and 6 minors fixed (see 1.13), 1 minor left (a Stop button, 1.14). Visuals (contrast, cut-off text at 150–200 %) were not judged — part E of the hand-test.

| # | Task | Done when |
|---|---|---|
| 1.1 | **Unpublished changes are never left behind.** A publish whose push failed (no internet, sign-in not finished) left the commit on the computer and the next publish said "Everything is up to date". Now, when the folder has nothing new, SEAGit checks for saved changes GitHub does not have (`rev-list @{u}..HEAD`, or no upstream yet) and sends them: "Publishing changes saved earlier that had not reached GitHub yet…" | ⚠️ Ron: hand-test B1. Tests: first publish never reached GitHub; later push failed |
| 1.2 | **First commit on a new PC works.** The bundled MinGit has no name/e-mail, so the first commit failed with "Author identity unknown". When none is set, the commit is made with `-c user.name=<Windows user name> -c user.email=seagit-user@seagit.invalid` (an address that can belong to no GitHub account); a name/e-mail the person set is always used instead | ⚠️ Ron: B2 (needs a PC/account with no Git set up). Tests: both cases |
| 1.3 | **Git no longer freezes the window or hangs.** Git runs asynchronously, output and errors are read while it runs (no pipe deadlock), arguments go through `ArgumentList` (no quoting problems), `GIT_TERMINAL_PROMPT=0` (no invisible console question), stdin closed. Status line: "Publishing "{folder}"… a GitHub sign-in window may open in your browser. The buttons come back when SEAGit has finished."; buttons off while it works; closing during a publish asks first | ⚠️ Ron: B3. Test: 3000 files publish without hanging |
| 1.4 | **Every outcome in a message box, in plain words.** Success ("Published N files to GitHub."), nothing to publish, empty folder, and failures explained (`PlainWords.ExplainGitError`): GitHub repository not empty / rejected; not found / 403; no internet; sign-in not finished; file over 100 MB; dubious ownership; anything else points to the Activity Log. Git's raw text stays in the Activity Log | ⚠️ Ron: B4. Tests: each message |
| 1.5 | **Publishes whatever the branch is called** (`push -u origin HEAD`), so an existing folder on `master` works. New folders are still made on `main`. README's GitHub Pages step now says to pick the branch SEAGit published to | ⚠️ Ron: B5. Test: master |
| 1.6 | **The GitHub address window.** Cancel (and Esc); the address is checked (must be `https://github.com/owner/name`; `.git`, a trailing `/` or a missing `https://` are tidied) and a wrong one says why in red under the box; the steps for making an empty GitHub repository (+ ▸ New repository, leave README/.gitignore/license unticked, copy the address); Help (F1). New **Change GitHub Address…** button (`remote set-url`, then the upstream is cleared so the whole folder goes to the new GitHub repository) | ⚠️ Ron: B6. Tests: address checks; change of address |
| 1.7 | **Main window.** "Your folders" heading; first-run instructions at the top ("1. Add a folder from your computer. 2. Select it in the list. 3. Press Publish Selected …"); **Open Folder** (and double-click); **Remove from List** (says your files are not deleted and GitHub is unchanged); list rows "name — last published … — path" | ⚠️ Ron: B7, and look at it at 150 % (E) |
| 1.8 | **Nothing dangerous is published by accident.** Plain prepare question ("SEAGit has not used this folder before. Prepare it for publishing? Everything inside it will be copied to GitHub when you press Publish Selected."); before a folder's first publish "N files (X MB) in "{folder}" will be published to the GitHub repository … Publish now?"; drive roots, the user-profile folder itself and Windows' own folders refused; files over 100 MB stop the publish **before** anything is saved (a commit holding one could never be pushed) | ⚠️ Ron: B8. Tests: refusals; large file found |
| 1.9 | **Minors.** AboutForm derives from DpiAwareForm (and its rounded picture follows the DPI scaling); "Select a folder in the list first, then press Publish Selected."; a damaged `repos.json` says so and keeps a copy (`repos.unreadable-<time>.json`) instead of silently emptying the list, and the list is saved through a temporary file; the list's Format handler is subscribed once; Check for a new version no longer shows exception text or enum names, and says the same thing before and after installing ("Windows closes SEAGit to install it; open it again from the Start menu."); one word for each thing ("folder", "GitHub repository", "Publish"); message boxes titled "SEAGit" / "SEAGit — …" | ⚠️ Ron: B9. Test: damaged list |
| 1.10 | **Usability pass (UX review 2026-10-31).** Help menu in the standard order: Help for this screen (F1) · Help on the web (softwareplus.ai/seagit) · Check for a new version… · About SEAGit…; Help for this screen in the main window and the address window, each linking to the matching softwareplus.ai/seagit/help page; tooltips on every button, the list, the log and the menu items; access keys (Alt+A, P, O, C, R); "Asking the Microsoft Store…" while it checks; About gets the privacy-policy link; Publish Selected a darker green (white text readable) | ⚠️ Ron: B10, C, D, E |
| 1.11 | **Version 1.0.6** (Ron decided 2026-10-31: 1.0.6, package 1.0.6.0; csproj `<Version>` and `WapProjTemplate/Package.appxmanifest` — the only two places it lives); hand-test exe `manual-test\SEAGit.exe` built 2026-10-31 | ⚠️ Ron: hand-test round 2026-11 in `MANUAL-TESTING.md` |
| 1.12 | **SoftwarePlus.ai help matches.** The help pages (`site/seagit/help/publish.html`, `add-folder.html`, `problems.html`, `about.html`) still quote the old words ("Saved Repositories", "Please select a repository…", "file(s) successfully pushed", "no way to change it in SEAGit", "the branch called main", the commit text "Auto-published via SEAGit" — now "Published with SEAGit"). Update them with the release, then `deploy.ps1` | ⬜ with the 1.0.6 release |
| 1.13 | **Fixes from the UX review of this round (UX review 2026-10-31).** Change GitHub Address… on a folder never linked no longer says "Link and Publish" (it says Save Address; the window title says which it is: "SEAGit — Link to GitHub" / "SEAGit — Change GitHub Address"); a refusal from GitHub on a folder published before explains that GitHub has changes not in the folder (edited on github.com, or another computer) instead of telling the person to make a new repository; the status line says what happens if the browser sign-in is closed; the same address written without `.git` is not treated as a change; an SSH address (`git@github.com:…`), `http://` and `www.github.com` are turned into the HTTPS address; a folder added again shows when it was really last published; "Not added: … was not prepared" in the log after No; an empty list says "No folders yet. Press + Add Local Folder… …" (click it to add); "+ Add Local Folder…" with an ellipsis; the window title "SEAGit — Super-Easy and Accessible Git"; Check for a new version turns the buttons off while it asks; a large-file refusal is never mistaken for a 403 | ⚠️ Ron: B11 |
| 1.14 | A **Stop** button for a publish waiting on sign-in (UX review minor; today closing SEAGit, which asks first, is the way out) | ⬜ next version |
| 1.15 | **Found taking the help screenshots (UX review 2026-10-31).** Long paths in **Your folders** ran past the right edge with a sideways scrollbar: the path part of each row is now shortened in the middle with "…" to fit the list's width (re-fitted when the window is resized; `PlainWords.ShortenMiddle`, 5 new tests, 38 green), the scrollbar is off, and resting the mouse on a row shows its full place (Open Folder and double-click still use the full path). The bold status line was blank when idle, leaving a gap above the Activity Log: it now says "Ready. Select a folder, then press Publish Selected." (or, with an empty list, "Ready. Press + Add Local Folder… to start."). `manual-test\SEAGit.exe` rebuilt; SoftwarePlus.ai help screenshots retaken with it | ⚠️ Ron: B12 |

---

## Phase 2 — the 1.0.6 release ⬜

| # | Task | Done when |
|---|---|---|
| 2.1 | Ron's hand-test round 2026-11 passes | ⚠️ Ron |
| 2.2 | Store package 1.0.6.0 built (`msix/build-msix.ps1`, `DEPLOY_TO_WIN_APP_STORE.md`); WACK | ⬜ |
| 2.3 | Store listing: "no network use" softened (the update check and publishing go online only when asked); privacy policy question 1 answered | ⚠️ **Wording changed 2026-10-31 per Ron (reflect the reality); ⚠️ Ron to read the page** — the policy moved to softwareplus.ai/seagit/privacy/ (new page; `PRIVACY_POLICY.txt` is the same text): Git built in, publishing only to the linked repository on Publish Selected, the name and e-mail on commits, GitHub's own browser sign-in, Check for a new version, the browser links, contact ron.eaglin@gmail.com (the placeholder is gone); effective October 31, 2026, version 1.0.6. Ron: set Partner Center's privacy URL to the new address, and soften the listing's "no network use" (its text is not in this repo) |
| 2.4 | Submitted; when live, tag `v1.0.6` and update SoftwarePlus.ai (1.12) | ⬜ |

### Release notes 1.0.6 (paste-ready, standing until the release)

```
SEAGit 1.0.6
- Changes are never left behind: if a publish could not reach GitHub, the next Publish Selected sends what was missed.
- Works on a new computer straight away (no more "Author identity unknown").
- The window keeps responding while publishing, and SEAGit never waits on a hidden question.
- Every result is shown in a message in plain words, with what to do next.
- Publishes folders whose branch is called master as well as main.
- The GitHub address window explains how to make an empty GitHub repository, checks the address, and has Cancel.
- New: Change GitHub Address…, Open Folder and Remove from List (your files are not deleted).
- Safer: asks before preparing a folder, shows how many files will be published the first time, refuses a whole drive or
  your user folder, and stops before a file over 100 MB.
- Help for this screen (F1), Help on the web, and tooltips on every button.
```

---

## Open questions for Ron

1. **(asked 2026-10-31) The privacy policy needs your wording.** `PRIVACY_POLICY.txt` (the one the Store listing and
   About link to) still says Git for Windows is installed separately (it is built into SEAGit now), has a placeholder
   "[add a support email address here before publishing]", and does not mention that Help ▸ Check for a new version
   asks the Microsoft Store. Should it move to softwareplus.ai/seagit/privacy/ like the other programs, with those three
   fixed? Not changed without you.
   **Answered 2026-10-31 (Ron: "Change wording to reflect the reality of the programs"):** yes — moved to
   softwareplus.ai/seagit/privacy/ with all three fixed (task 2.3).
2. **(asked 2026-10-31) The name on commits.** On a computer with no Git name set, commits are signed with the Windows
   user name and `seagit-user@seagit.invalid`, so GitHub shows them without an avatar. Would you rather SEAGit ask once
   for "the name to show on GitHub"?

---

## References

- [`MANUAL-TESTING.md`](MANUAL-TESTING.md) — the hand-test build command and the round 2026-11 steps (A–E).
- [`../README.md`](../README.md) — the end-user walkthrough on GitHub (Pages step now names the branch).
- [`../DEPLOY_TO_WIN_APP_STORE.md`](../DEPLOY_TO_WIN_APP_STORE.md) — building and submitting the Store package.
- [`../msix/README.md`](../msix/README.md) — the MSIX build script.
- [`../PRIVACY_POLICY.txt`](../PRIVACY_POLICY.txt) — the privacy policy (question 1).
- [`../../../USABILITY-CHECKLIST.md`](../../../USABILITY-CHECKLIST.md) — the usability rules every Store program meets.
