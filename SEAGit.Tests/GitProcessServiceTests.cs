using SEAGit.Services;

namespace SEAGit.Tests;

/// <summary>
/// SEAGit's Git steps, run with the bundled MinGit against a bare repository in %TEMP% that stands in for GitHub.
/// The computer's own Git settings are hidden (no name or e-mail, as on a new PC with only SEAGit installed).
/// </summary>
public sealed class GitProcessServiceTests : IDisposable
{
    private readonly string _root;
    private readonly GitProcessService _git;

    public GitProcessServiceTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "seagit-tests", Guid.NewGuid().ToString("N"));
        string home = Path.Combine(_root, "home");
        Directory.CreateDirectory(home);
        File.WriteAllText(Path.Combine(home, ".gitconfig"), "");
        _git = new GitProcessService(FindGit(), new Dictionary<string, string>
        {
            ["HOME"] = home,
            ["USERPROFILE"] = home,
            ["XDG_CONFIG_HOME"] = home,
            ["GIT_CONFIG_NOSYSTEM"] = "1",
            ["GIT_CONFIG_GLOBAL"] = Path.Combine(home, ".gitconfig"),
        });
    }

    public void Dispose()
    {
        try
        {
            foreach (var f in Directory.EnumerateFiles(_root, "*", SearchOption.AllDirectories))
                File.SetAttributes(f, FileAttributes.Normal);
            Directory.Delete(_root, recursive: true);
        }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }

    private static string FindGit()
    {
        // The test output carries MinGit (copied from the SEAGit project); fall back to the source tree.
        string inOutput = Path.Combine(AppContext.BaseDirectory, "MinGit", "cmd", "git.exe");
        if (File.Exists(inOutput)) return inOutput;
        for (var d = new DirectoryInfo(AppContext.BaseDirectory); d != null; d = d.Parent)
        {
            string candidate = Path.Combine(d.FullName, "SEAGit", "MinGit", "cmd", "git.exe");
            if (File.Exists(candidate)) return candidate;
        }
        throw new FileNotFoundException("Bundled MinGit not found");
    }

    private async Task<string> NewBareAsync(string name)
    {
        string bare = Path.Combine(_root, name + ".git");
        Directory.CreateDirectory(bare);
        Assert.True((await _git.RunAsync(bare, "init", "--bare")).Ok);
        return bare.Replace('\\', '/');
    }

    private async Task<string> NewFolderAsync(string name, bool prepare = true)
    {
        string folder = Path.Combine(_root, name);
        Directory.CreateDirectory(folder);
        if (prepare) Assert.True((await _git.InitializeRepoAsync(folder)).Ok);
        return folder;
    }

    private async Task<string> BareBranchesAsync(string bare) =>
        (await _git.RunAsync(bare, "for-each-ref", "--format=%(refname)", "refs/heads")).Output.Trim();

    [Fact]
    public async Task First_publish_on_a_new_PC_works_without_a_Git_name_or_email()
    {
        string bare = await NewBareAsync("hub");
        string folder = await NewFolderAsync("site");
        File.WriteAllText(Path.Combine(folder, "index.html"), "<h1>Hi</h1>");
        File.WriteAllText(Path.Combine(folder, "about.html"), "<p>About</p>");
        Assert.True((await _git.SetRemoteAsync(folder, bare)).Ok);

        Assert.False(await _git.HasBeenPublishedAsync(folder));
        var pending = await _git.GetFilesToPublishAsync(folder, firstPublish: true);
        Assert.Equal(2, pending.Count);

        var result = await _git.PublishAsync(folder);
        Assert.Equal(PublishOutcome.Published, result.Outcome);
        Assert.Equal(2, result.FileCount);
        Assert.Equal("refs/heads/main", await BareBranchesAsync(bare));

        string author = (await _git.RunAsync(bare, "log", "-1", "--format=%ae", "main")).Output.Trim();
        Assert.Equal(GitProcessService.FallbackEmail, author);
        Assert.True(await _git.HasBeenPublishedAsync(folder));
        var when = await _git.LastPublishedAsync(folder);
        Assert.NotNull(when);
        Assert.True(Math.Abs((DateTime.Now - when!.Value).TotalMinutes) < 5);
    }

    [Fact]
    public async Task A_name_and_email_the_person_set_are_used_instead_of_the_fallback()
    {
        string bare = await NewBareAsync("hub");
        string folder = await NewFolderAsync("work");
        await _git.RunAsync(folder, "config", "user.name", "Pat Person");
        await _git.RunAsync(folder, "config", "user.email", "pat@example.com");
        File.WriteAllText(Path.Combine(folder, "a.txt"), "a");
        await _git.SetRemoteAsync(folder, bare);

        Assert.Equal(PublishOutcome.Published, (await _git.PublishAsync(folder)).Outcome);
        Assert.Equal("pat@example.com", (await _git.RunAsync(bare, "log", "-1", "--format=%ae", "main")).Output.Trim());
    }

    [Fact]
    public async Task Nothing_new_says_nothing_to_publish_and_an_empty_folder_says_so()
    {
        string bare = await NewBareAsync("hub");
        string empty = await NewFolderAsync("empty");
        await _git.SetRemoteAsync(empty, bare);
        Assert.Equal(PublishOutcome.FolderEmpty, (await _git.PublishAsync(empty)).Outcome);

        File.WriteAllText(Path.Combine(empty, "a.txt"), "a");
        Assert.Equal(PublishOutcome.Published, (await _git.PublishAsync(empty)).Outcome);
        Assert.Equal(PublishOutcome.NothingToPublish, (await _git.PublishAsync(empty)).Outcome);
    }

    [Fact]
    public async Task A_first_publish_that_never_reached_GitHub_is_sent_by_the_next_publish()
    {
        string bare = await NewBareAsync("hub");
        string folder = await NewFolderAsync("site");
        File.WriteAllText(Path.Combine(folder, "index.html"), "x");
        await _git.SetRemoteAsync(folder, Path.Combine(_root, "missing.git").Replace('\\', '/'));

        var failed = await _git.PublishAsync(folder);
        Assert.Equal(PublishOutcome.Failed, failed.Outcome);
        Assert.False(string.IsNullOrWhiteSpace(failed.RawError));
        Assert.DoesNotContain("plain words", PlainWords.ExplainGitError(failed.RawError)); // a known case, not the fallback

        // The commit stayed on this computer; the folder itself is clean now.
        Assert.True((await _git.SetRemoteAsync(folder, bare)).Ok);
        var retry = await _git.PublishAsync(folder);
        Assert.Equal(PublishOutcome.Published, retry.Outcome);
        Assert.Equal(1, retry.FileCount);
        Assert.Equal("refs/heads/main", await BareBranchesAsync(bare));
    }

    [Fact]
    public async Task Changes_whose_push_failed_later_are_sent_by_the_next_publish()
    {
        string bare = await NewBareAsync("hub");
        string folder = await NewFolderAsync("site");
        File.WriteAllText(Path.Combine(folder, "index.html"), "v1");
        await _git.SetRemoteAsync(folder, bare);
        Assert.Equal(PublishOutcome.Published, (await _git.PublishAsync(folder)).Outcome);

        // GitHub cannot be reached for one publish (the address breaks), then comes back.
        await _git.RunAsync(folder, "remote", "set-url", "origin", Path.Combine(_root, "gone.git").Replace('\\', '/'));
        File.WriteAllText(Path.Combine(folder, "index.html"), "v2");
        Assert.Equal(PublishOutcome.Failed, (await _git.PublishAsync(folder)).Outcome);
        await _git.RunAsync(folder, "remote", "set-url", "origin", bare);

        var progressNotes = new List<string>();
        var resend = await _git.PublishAsync(folder, new SyncProgress(progressNotes.Add));
        Assert.Equal(PublishOutcome.PublishedEarlierChanges, resend.Outcome);
        Assert.Equal(1, resend.FileCount);
        Assert.Contains(progressNotes, n => n.Contains("had not reached GitHub yet"));
        Assert.Equal("v2", (await _git.RunAsync(bare, "show", "main:index.html")).Output);
        Assert.Equal(PublishOutcome.NothingToPublish, (await _git.PublishAsync(folder)).Outcome);
    }

    [Fact]
    public async Task An_existing_folder_on_master_publishes_to_master()
    {
        string bare = await NewBareAsync("hub");
        string folder = await NewFolderAsync("old", prepare: false);
        await _git.RunAsync(folder, "init");
        await _git.RunAsync(folder, "symbolic-ref", "HEAD", "refs/heads/master");
        File.WriteAllText(Path.Combine(folder, "notes.txt"), "n");
        await _git.SetRemoteAsync(folder, bare);

        Assert.Equal(PublishOutcome.Published, (await _git.PublishAsync(folder)).Outcome);
        Assert.Equal("refs/heads/master", await BareBranchesAsync(bare));
    }

    [Fact]
    public async Task Changing_the_GitHub_address_sends_the_whole_folder_to_the_new_repository()
    {
        string bare1 = await NewBareAsync("hub1");
        string bare2 = await NewBareAsync("hub2");
        string folder = await NewFolderAsync("site");
        File.WriteAllText(Path.Combine(folder, "index.html"), "x");
        await _git.SetRemoteAsync(folder, bare1);
        await _git.PublishAsync(folder);

        // What Change GitHub Address… does.
        Assert.True((await _git.SetRemoteAsync(folder, bare2)).Ok);
        await _git.RunAsync(folder, "branch", "--unset-upstream");
        Assert.Equal(bare2, await _git.GetRemoteUrlAsync(folder));
        Assert.False(await _git.HasBeenPublishedAsync(folder));

        var r = await _git.PublishAsync(folder);
        Assert.Equal(PublishOutcome.Published, r.Outcome);
        Assert.Equal(1, r.FileCount);
        Assert.Equal("refs/heads/main", await BareBranchesAsync(bare2));
    }

    [Fact]
    public async Task Files_over_100_MB_are_found_before_anything_is_saved()
    {
        string folder = await NewFolderAsync("big");
        using (var fs = File.Create(Path.Combine(folder, "video.mp4")))
            fs.SetLength(GitProcessService.GitHubFileLimitBytes + 1);
        File.WriteAllText(Path.Combine(folder, "small.txt"), "s");

        var pending = await _git.GetFilesToPublishAsync(folder, firstPublish: true);
        var big = Assert.Single(pending, f => f.Bytes > GitProcessService.GitHubFileLimitBytes);
        Assert.Equal("video.mp4", big.RelativePath);

        var changed = await _git.GetFilesToPublishAsync(folder, firstPublish: false);
        Assert.Equal(2, changed.Count);
    }

    [Fact]
    public async Task A_lot_of_output_does_not_hang()
    {
        // Thousands of files make Git print a lot; reading only after it ended used to be able to deadlock.
        string bare = await NewBareAsync("hub");
        string folder = await NewFolderAsync("many");
        for (int i = 0; i < 3000; i++) File.WriteAllText(Path.Combine(folder, $"file-with-a-long-name-{i:0000}.txt"), "x\n");
        await _git.SetRemoteAsync(folder, bare);

        var run = _git.PublishAsync(folder);
        var finished = await Task.WhenAny(run, Task.Delay(TimeSpan.FromMinutes(2)));
        Assert.Same(run, finished);
        Assert.Equal(3000, (await run).FileCount);
    }

    /// <summary>IProgress that reports straight away (Progress&lt;T&gt; posts later, on the thread pool).</summary>
    private sealed class SyncProgress(Action<string> report) : IProgress<string>
    {
        public void Report(string value) => report(value);
    }
}
