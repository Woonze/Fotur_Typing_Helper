using System.Text.RegularExpressions;

namespace FoturTypingHelper.Tests;

public sealed class VersionConsistencyTests
{
    [Fact]
    public void ReleaseFacingFilesUseVersion100()
    {
        var root = FindRepositoryRoot();
        const string version = "1.0.0";
        Assert.Contains($"<Version>{version}</Version>", Read(root, "Directory.Build.props"));
        Assert.Contains($"#define AppVersion \"{version}\"", Read(root, "installer/FoturTypingHelper.iss"));
        Assert.Contains($"version=\"{version}.0\"", Read(root, "src/FoturTypingHelper.App/app.manifest"));
        Assert.Contains($"Версия {version}", Read(root, "README.md"));
        Assert.Contains($"Fotur Typing Helper {version}", Read(root, "RELEASE_NOTES.md"));
        Assert.Contains($"Fotur Typing Helper {version}", Read(root, "docs/FUNCTIONS.md"));

        foreach (var file in Directory.EnumerateFiles(root, "*", SearchOption.AllDirectories)
                     .Where(path => !path.Contains($"{Path.DirectorySeparatorChar}.git{Path.DirectorySeparatorChar}") &&
                                    !path.Contains($"{Path.DirectorySeparatorChar}bin{Path.DirectorySeparatorChar}") &&
                                    !path.Contains($"{Path.DirectorySeparatorChar}obj{Path.DirectorySeparatorChar}") &&
                                    Path.GetExtension(path) is ".md" or ".cs" or ".axaml" or ".props" or ".iss" or ".manifest"))
        {
            Assert.DoesNotMatch(new Regex(@"\b0\.(?:1|2|3)\.\d+\b"), File.ReadAllText(file));
        }
    }

    private static string Read(string root, string relative) => File.ReadAllText(Path.Combine(root, relative));

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Directory.Build.props")))
            directory = directory.Parent;
        return directory?.FullName ?? throw new DirectoryNotFoundException("Repository root not found");
    }
}
