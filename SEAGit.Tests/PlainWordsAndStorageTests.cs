using SEAGit.Models;
using SEAGit.Services;

namespace SEAGit.Tests;

public class PlainWordsTests
{
    [Theory]
    [InlineData(" ! [rejected]        HEAD -> main (fetch first)\nerror: failed to push some refs", "already has files")]
    [InlineData("remote: Repository not found.\nfatal: repository 'https://github.com/a/b.git/' not found", "could not find")]
    [InlineData("remote: Permission to a/b.git denied to c.\nfatal: unable to access '...': The requested URL returned error: 403", "not allowed")]
    [InlineData("fatal: unable to access 'https://github.com/a/b.git/': Could not resolve host: github.com", "could not reach GitHub")]
    [InlineData("fatal: Authentication failed for 'https://github.com/a/b.git/'", "sign-in did not finish")]
    [InlineData("fatal: could not read Username for 'https://github.com': terminal prompts disabled", "sign-in did not finish")]
    [InlineData("remote: error: GH001: Large files detected.", "over 100 MB")]
    [InlineData("something nobody expected", "Activity Log")]
    public void Common_Git_failures_are_explained_in_plain_words(string raw, string expected)
    {
        Assert.Contains(expected, PlainWords.ExplainGitError(raw));
    }

    [Fact]
    public void A_refusal_after_earlier_publishes_does_not_say_make_a_new_repository()
    {
        const string raw = " ! [rejected]        HEAD -> main (fetch first)";
        Assert.Contains("new, empty GitHub repository", PlainWords.ExplainGitError(raw));
        string later = PlainWords.ExplainGitError(raw, publishedBefore: true);
        Assert.Contains("edited on github.com", later);
        Assert.DoesNotContain("new, empty", later);
    }

    [Fact]
    public void A_path_that_fits_is_not_shortened()
    {
        const string path = @"C:\Users\Public\Documents\Lab reports";
        Assert.Equal(path, PlainWords.ShortenMiddle(path, path.Length, s => s.Length));
    }

    [Theory]
    [InlineData(30)]
    [InlineData(20)]
    [InlineData(12)]
    public void A_long_path_is_shortened_in_the_middle_and_keeps_its_end(int width)
    {
        const string path = @"C:\Users\Public\Documents\SEAGit example\My class website";
        string shown = PlainWords.ShortenMiddle(path, width, s => s.Length);
        Assert.Equal(width, shown.Length);          // as much as fits
        Assert.Contains("…", shown);
        Assert.StartsWith("C:", shown);
        Assert.EndsWith("site", shown);             // more of the end (the folder's own name) is kept
        int dot = shown.IndexOf('…');
        Assert.True(shown.Length - dot - 1 >= dot, "the end keeps at least as much as the start");
    }

    [Fact]
    public void A_width_with_no_room_shows_only_the_dots()
    {
        Assert.Equal("…", PlainWords.ShortenMiddle(@"C:\folder", 0, s => s.Length));
    }

    [Fact]
    public void A_large_file_named_403_is_still_explained_as_a_large_file()
    {
        Assert.Contains("over 100 MB", PlainWords.ExplainGitError("remote: error: GH001: Large files detected. File video-403.mp4 is 120 MB"));
    }

    [Fact]
    public void A_whole_drive_and_the_user_folder_are_refused_but_a_folder_in_Documents_is_not()
    {
        Assert.NotNull(PlainWords.WhyFolderIsRefused(@"C:\"));
        Assert.NotNull(PlainWords.WhyFolderIsRefused(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)));
        Assert.NotNull(PlainWords.WhyFolderIsRefused(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "\\"));
        Assert.NotNull(PlainWords.WhyFolderIsRefused(Environment.GetFolderPath(Environment.SpecialFolder.Windows)));
        Assert.Null(PlainWords.WhyFolderIsRefused(Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "my-website")));
    }

    [Theory]
    [InlineData("https://github.com/ron/site", "https://github.com/ron/site.git")]
    [InlineData("https://github.com/ron/site.git", "https://github.com/ron/site.git")]
    [InlineData(" https://github.com/ron/site/ ", "https://github.com/ron/site.git")]
    [InlineData("github.com/ron/site", "https://github.com/ron/site.git")]
    [InlineData("git@github.com:ron/site.git", "https://github.com/ron/site.git")]
    [InlineData("http://www.github.com/ron/site", "https://github.com/ron/site.git")]
    public void GitHub_addresses_are_accepted_and_tidied(string input, string expected)
    {
        Assert.Equal(expected, PlainWords.NormaliseGitHubAddress(input, out _));
    }

    [Theory]
    [InlineData("")]
    [InlineData("www.example.com")]
    [InlineData("https://gitlab.com/ron/site")]
    [InlineData("https://github.com/ron")]
    [InlineData("https://github.com/ron/site/tree/main")]
    public void Other_addresses_are_refused_with_a_reason(string input)
    {
        Assert.Null(PlainWords.NormaliseGitHubAddress(input, out string problem));
        Assert.False(string.IsNullOrWhiteSpace(problem));
    }
}

public sealed class StorageServiceTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "seagit-tests", "storage-" + Guid.NewGuid().ToString("N"));

    public void Dispose()
    {
        try { Directory.Delete(_dir, recursive: true); } catch (IOException) { }
    }

    [Fact]
    public void The_list_is_saved_and_read_back()
    {
        var s = new StorageService(_dir);
        s.SaveRepositories(new List<GitRepository> { new() { Name = "site", LocalPath = @"C:\x\site" } });
        var back = new StorageService(_dir).LoadRepositories();
        Assert.Equal("site", Assert.Single(back).Name);
    }

    [Fact]
    public void A_damaged_list_is_reported_and_a_copy_is_kept()
    {
        var s = new StorageService(_dir);
        File.WriteAllText(s.FilePath, "{ this is not json");

        var list = s.LoadRepositories();
        Assert.Empty(list);
        Assert.True(s.ListWasUnreadable);
        Assert.NotNull(s.BrokenListBackupPath);
        Assert.Equal("{ this is not json", File.ReadAllText(s.BrokenListBackupPath!));
    }

    [Fact]
    public void The_name_on_GitHub_is_saved_beside_the_list_and_read_back()
    {
        var s = new StorageService(_dir);
        Assert.Null(s.LoadIdentity());
        s.SaveIdentity(new CommitIdentity { Name = "  Pat Smith ", Email = " pat@users.noreply.github.com " });
        Assert.Equal(Path.GetDirectoryName(s.FilePath), Path.GetDirectoryName(s.IdentityFilePath));
        Assert.False(File.Exists(s.IdentityFilePath + ".tmp"));

        var back = new StorageService(_dir).LoadIdentity();
        Assert.NotNull(back);
        Assert.Equal("Pat Smith", back!.Name);
        Assert.Equal("pat@users.noreply.github.com", back.Email);
    }

    [Fact]
    public void A_damaged_name_file_means_SEAGit_asks_again()
    {
        var s = new StorageService(_dir);
        File.WriteAllText(s.IdentityFilePath, "{ broken");
        Assert.Null(s.LoadIdentity());
    }
}

public class IdentityChoiceTests
{
    private static readonly CommitIdentity Saved = new() { Name = "Pat Smith", Email = "pat@users.noreply.github.com" };

    [Fact]
    public void Git_config_wins_and_no_name_is_passed()
    {
        var d = GitProcessService.DecideIdentity("Git Name", "git@example.com", Saved);
        Assert.Equal(IdentitySource.GitConfig, d.Source);
        Assert.Empty(d.Arguments);
    }

    [Fact]
    public void Git_name_without_an_email_takes_the_saved_email()
    {
        var d = GitProcessService.DecideIdentity("Git Name", "", Saved);
        Assert.Equal(IdentitySource.GitConfig, d.Source);
        Assert.Equal(new[] { "-c", "user.email=pat@users.noreply.github.com" }, d.Arguments);
    }

    [Fact]
    public void The_saved_setting_is_used_when_Git_has_no_name()
    {
        var d = GitProcessService.DecideIdentity("", "", Saved);
        Assert.Equal(IdentitySource.SavedInSeaGit, d.Source);
        Assert.Equal(new[] { "-c", "user.name=Pat Smith", "-c", "user.email=pat@users.noreply.github.com" }, d.Arguments);
    }

    [Fact]
    public void A_saved_name_without_an_email_uses_the_placeholder_and_a_Git_email_still_wins()
    {
        var noEmail = new CommitIdentity { Name = "Pat Smith" };
        Assert.Equal(new[] { "-c", "user.name=Pat Smith", "-c", "user.email=" + GitProcessService.FallbackEmail },
            GitProcessService.DecideIdentity("", "", noEmail).Arguments);
        Assert.Equal(new[] { "-c", "user.name=Pat Smith" }, GitProcessService.DecideIdentity("", "git@example.com", Saved).Arguments);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void No_name_anywhere_means_ask(string? savedName)
    {
        var saved = savedName == null ? null : new CommitIdentity { Name = savedName };
        Assert.Equal(IdentitySource.AskThePerson, GitProcessService.DecideIdentity("", "", saved).Source);
    }

    [Theory]
    [InlineData("Pat Smith", "", true)]
    [InlineData("Pat Smith", "pat@users.noreply.github.com", true)]
    [InlineData("Pat Smith", "pat@example.co.uk", true)]
    [InlineData("", "pat@example.com", false)]
    [InlineData("   ", "", false)]
    [InlineData("Pat Smith", "pat", false)]
    [InlineData("Pat Smith", "pat@example", false)]
    [InlineData("Pat Smith", "pat.smith@", false)]
    [InlineData("Pat Smith", "@example.com", false)]
    [InlineData("Pat Smith", "pat <pat@example.com>", false)]
    public void The_name_is_required_and_the_email_is_checked_loosely(string name, string email, bool ok)
    {
        string? problem = PlainWords.WhatIsWrongWithIdentity(name, email, out bool emailIsTheProblem);
        Assert.Equal(ok, problem == null);
        if (!ok) Assert.Equal(!string.IsNullOrWhiteSpace(name), emailIsTheProblem);
    }

    [Theory]
    [InlineData(".")]
    [InlineData("--")]
    [InlineData("Pat <Smith>")]
    public void A_name_Git_would_refuse_is_caught_first(string name)
    {
        Assert.NotNull(PlainWords.WhatIsWrongWithIdentity(name, "", out bool emailIsTheProblem));
        Assert.False(emailIsTheProblem);
    }
}
