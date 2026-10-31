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
}
