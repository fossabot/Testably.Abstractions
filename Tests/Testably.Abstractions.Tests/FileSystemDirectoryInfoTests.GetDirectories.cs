using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Testably.Abstractions.Tests;

public abstract partial class FileSystemDirectoryInfoTests<TFileSystem>
    where TFileSystem : IFileSystem
{
    [Theory]
    [AutoData]
    [FileSystemTests.DirectoryInfo(
        nameof(IFileSystem.IDirectoryInfo.GetDirectories))]
    public void
        GetDirectories_SearchOptionAllDirectories_ShouldReturnAllSubdirectories(
            string path)
    {
        FileSystemInitializer.IFileSystemDirectoryInitializer<TFileSystem> initialized =
            FileSystem.Initialize()
               .WithSubdirectory(path).Initialized(s => s
                   .WithSubdirectory("foo/xyz")
                   .WithSubdirectory("bar"));
        var baseDirectory = (IFileSystem.IDirectoryInfo)initialized[0];

        IFileSystem.IDirectoryInfo[] result = baseDirectory
           .GetDirectories("*", SearchOption.AllDirectories);

        result.Length.Should().Be(3);
        result.Should().Contain(d => d.Name == "foo");
        result.Should().Contain(d => d.Name == "bar");
        result.Should().Contain(d => d.Name == "xyz");
    }

    [Theory]
#if NETFRAMEWORK
    [InlineAutoData(false, "")]
#else
    [InlineAutoData(true, "")]
#endif
    [InlineAutoData(true, "*")]
    [InlineAutoData(true, ".")]
    [InlineAutoData(true, "*.*")]
    [InlineData(true, "a*c", "abc")]
    [InlineData(true, "ab*c", "abc")]
    [InlineData(true, "abc?", "abc")]
    [InlineData(false, "ab?c", "abc")]
    [InlineData(false, "ac", "abc")]
    [FileSystemTests.DirectoryInfo(
        nameof(IFileSystem.IDirectoryInfo.GetDirectories))]
    public void GetDirectories_SearchPattern_ShouldReturnExpectedValue(
        bool expectToBeFound, string searchPattern, string subdirectoryName)
    {
        IFileSystem.IDirectoryInfo baseDirectory =
            FileSystem.Directory.CreateDirectory("foo");
        baseDirectory.CreateSubdirectory(subdirectoryName);

        IFileSystem.IDirectoryInfo[] result = baseDirectory
           .GetDirectories(searchPattern);

        if (expectToBeFound)
        {
            result.Should().ContainSingle(d => d.Name == subdirectoryName,
                $"it should match '{searchPattern}'");
        }
        else
        {
            result.Should()
               .BeEmpty($"{subdirectoryName} should not match '{searchPattern}'");
        }
    }

#if FEATURE_FILESYSTEM_ENUMERATION_OPTIONS
    [Theory]
    [AutoData]
    [FileSystemTests.DirectoryInfo(
        nameof(IFileSystem.IDirectoryInfo.GetDirectories))]
    public void
        GetDirectories_WithEnumerationOptions_ShouldConsiderSetOptions(
            string path)
    {
        IFileSystem.IDirectoryInfo baseDirectory =
            FileSystem.Directory.CreateDirectory(path);
        baseDirectory.CreateSubdirectory("foo/xyz");
        baseDirectory.CreateSubdirectory("bar");

        IFileSystem.IDirectoryInfo[] result = baseDirectory
           .GetDirectories("XYZ",
                new EnumerationOptions
                {
                    MatchCasing = MatchCasing.CaseInsensitive,
                    RecurseSubdirectories = true,
                    // Filename could start with a leading '.' indicating it as Hidden in Linux
                    AttributesToSkip = FileAttributes.System
                });

        result.Length.Should().Be(1);
        result.Should().NotContain(d => d.Name == "foo");
        result.Should().Contain(d => d.Name == "xyz");
        result.Should().NotContain(d => d.Name == "bar");
    }
#endif

    [Theory]
    [AutoData]
    [FileSystemTests.DirectoryInfo(
        nameof(IFileSystem.IDirectoryInfo.GetDirectories))]
    public void GetDirectories_WithNewline_ShouldThrowArgumentException(
        string path)
    {
        IFileSystem.IDirectoryInfo baseDirectory =
            FileSystem.DirectoryInfo.New(path);
        string searchPattern = "foo\0bar";

        Exception? exception = Record.Exception(() =>
        {
            _ = baseDirectory.GetDirectories(searchPattern).FirstOrDefault();
        });

        exception.Should().BeOfType<ArgumentException>();
    }

    [Theory]
    [AutoData]
    [FileSystemTests.DirectoryInfo(
        nameof(IFileSystem.IDirectoryInfo.GetDirectories))]
    public void
        GetDirectories_WithoutSearchString_ShouldReturnAllDirectSubdirectories(
            string path)
    {
        IFileSystem.IDirectoryInfo baseDirectory =
            FileSystem.Directory.CreateDirectory(path);
        baseDirectory.CreateSubdirectory("foo/xyz");
        baseDirectory.CreateSubdirectory("bar");

        IFileSystem.IDirectoryInfo[] result = baseDirectory
           .GetDirectories();

        result.Length.Should().Be(2);
        result.Should().Contain(d => d.Name == "foo");
        result.Should().NotContain(d => d.Name == "xyz");
        result.Should().Contain(d => d.Name == "bar");
    }

    [Theory]
    [AutoData]
    [FileSystemTests.DirectoryInfo(
        nameof(IFileSystem.IDirectoryInfo.GetDirectories))]
    public void GetDirectories_WithSearchPattern_ShouldReturnMatchingSubdirectory(
        string path)
    {
        IFileSystem.IDirectoryInfo baseDirectory =
            FileSystem.Directory.CreateDirectory(path);
        baseDirectory.CreateSubdirectory("foo");
        baseDirectory.CreateSubdirectory("bar");

        IEnumerable<IFileSystem.IDirectoryInfo> result = baseDirectory
           .GetDirectories("foo");

        result.Should().ContainSingle(d => d.Name == "foo");
    }

    [Theory]
    [AutoData]
    [FileSystemTests.DirectoryInfo(
        nameof(IFileSystem.IDirectoryInfo.GetDirectories))]
    public void
        GetDirectories_WithSearchPatternInSubdirectory_ShouldReturnMatchingSubdirectory(
            string path)
    {
        IFileSystem.IDirectoryInfo baseDirectory =
            FileSystem.Directory.CreateDirectory(path);
        baseDirectory.CreateSubdirectory("foo/xyz");
        baseDirectory.CreateSubdirectory("bar/xyz");

        IEnumerable<IFileSystem.IDirectoryInfo> result = baseDirectory
           .GetDirectories("xyz", SearchOption.AllDirectories);

        result.Count().Should().Be(2);
    }
}