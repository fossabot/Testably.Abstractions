using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Testably.Abstractions.Tests;

public abstract partial class FileSystemDirectoryTests<TFileSystem>
    where TFileSystem : IFileSystem
{
    [Theory]
    [AutoData]
    public void
        EnumerateFileSystemEntries_MissingDirectory_ShouldThrowDirectoryNotFoundException(
            string path)
    {
        string expectedPath = Path.Combine(BasePath, path);
        Exception? exception =
            Record.Exception(()
                => FileSystem.Directory.EnumerateFileSystemEntries(path).ToList());

        exception.Should().BeOfType<DirectoryNotFoundException>()
           .Which.Message.Should().Contain($"'{expectedPath}'.");
        FileSystem.Directory.Exists(path).Should().BeFalse();
    }

    [Theory]
    [AutoData]
    public void
        EnumerateFileSystemEntries_SearchOptionAllDirectories_FullPath_ShouldReturnAllFileSystemEntriesWithFullPath(
            string path)
    {
        IFileSystem.IDirectoryInfo baseDirectory =
            FileSystem.Directory.CreateDirectory(path);
        IFileSystem.IFileInfo file1 = FileSystem.GenerateRandomFile(path);
        IFileSystem.IDirectoryInfo subdirectory = FileSystem
           .GenerateRandomSubdirectory(path);
        IFileSystem.IFileInfo file2 = FileSystem
           .GenerateRandomFile(subdirectory.FullName);

        List<string> result = FileSystem.Directory
           .EnumerateFileSystemEntries(baseDirectory.FullName, "*",
                SearchOption.AllDirectories)
           .ToList();

        result.Count.Should().Be(3);
        result.Should().Contain(file1.FullName);
        result.Should().Contain(subdirectory.FullName);
        result.Should().Contain(file2.FullName);
    }

    [Theory]
    [AutoData]
    public void
        EnumerateFileSystemEntries_SearchOptionAllDirectories_ShouldReturnAllFileSystemEntries(
            string path)
    {
        IFileSystem.IFileInfo file1 = FileSystem.GenerateRandomFile(path);
        IFileSystem.IDirectoryInfo subdirectory = FileSystem
           .GenerateRandomSubdirectory(path);
        IFileSystem.IFileInfo file2 = FileSystem
           .GenerateRandomFile(subdirectory.FullName);

        List<string> result = FileSystem.Directory
           .EnumerateFileSystemEntries(path, "*", SearchOption.AllDirectories)
           .ToList();

        result.Count.Should().Be(3);
        result.Should().Contain(FileSystem.Path.Combine(path, file1.Name));
        result.Should()
           .Contain(FileSystem.Path.Combine(path, subdirectory.Name));
        result.Should()
           .Contain(FileSystem.Path.Combine(path, subdirectory.Name, file2.Name));
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
    public void EnumerateFileSystemEntries_SearchPattern_ShouldReturnExpectedValue(
        bool expectToBeFound, string searchPattern, string fileName)
    {
        FileSystem.GenerateFile(fileName);

        List<string> result = FileSystem.Directory
           .EnumerateFileSystemEntries(".", searchPattern).ToList();

        if (expectToBeFound)
        {
            result.Should().ContainSingle(
                fileName,
                $"it should match {searchPattern}");
        }
        else
        {
            result.Should()
               .BeEmpty($"{fileName} should not match {searchPattern}");
        }
    }

#if FEATURE_FILESYSTEM_ENUMERATION_OPTIONS
    [Theory]
    [AutoData]
    public void
        EnumerateFileSystemEntries_WithEnumerationOptions_ShouldConsiderSetOptions(
            string path)
    {
        IFileSystem.IFileInfo file1 = FileSystem.GenerateRandomFile(path);
        IFileSystem.IDirectoryInfo subdirectory = FileSystem
           .GenerateRandomSubdirectory(path);
        IFileSystem.IFileInfo file2 = FileSystem
           .GenerateRandomFile(subdirectory.FullName);

        List<string> result = FileSystem.Directory
           .EnumerateFileSystemEntries(path, file2.Name.ToUpper(),
                new EnumerationOptions
                {
                    MatchCasing = MatchCasing.CaseInsensitive,
                    RecurseSubdirectories = true
                }).ToList();

        result.Count.Should().Be(1);
        result.Should().NotContain(FileSystem.Path.Combine(path, file1.Name));
        result.Should()
           .Contain(FileSystem.Path.Combine(path, subdirectory.Name, file2.Name));
    }
#endif

    [Theory]
    [AutoData]
    public void EnumerateFileSystemEntries_WithNewline_ShouldThrowArgumentException(
        string path)
    {
        string searchPattern = "foo\0bar";

        Exception? exception = Record.Exception(() =>
        {
            _ = FileSystem.Directory.EnumerateFileSystemEntries(path, searchPattern)
               .FirstOrDefault();
        });

#if NETFRAMEWORK
        // The searchPattern is not included in .NET Framework
        exception.Should().BeOfType<ArgumentException>();
#else
        exception.Should().BeOfType<ArgumentException>()
           .Which.Message.Should().Contain($"'{searchPattern}'");
#endif
    }

    [Theory]
    [AutoData]
    public void
        EnumerateFileSystemEntries_WithoutSearchString_ShouldReturnAllFileSystemEntriesInDirectSubdirectories(
            string path)
    {
        IFileSystem.IFileInfo foundFile1 = FileSystem.GenerateRandomFile(path);
        IFileSystem.IFileInfo foundFile2 = FileSystem.GenerateRandomFile(path);
        IFileSystem.IDirectoryInfo foundSubdirectory =
            FileSystem.GenerateRandomSubdirectory(path);
        IFileSystem.IFileInfo notFoundFile =
            FileSystem.GenerateRandomFile(foundSubdirectory.FullName);

        List<string> result =
            FileSystem.Directory.EnumerateFileSystemEntries(path).ToList();

        result.Count.Should().Be(3);
        result.Should().Contain(foundFile1.ToString());
        result.Should().Contain(foundFile2.ToString());
        result.Should().Contain(foundSubdirectory.ToString());
        result.Should().NotContain(notFoundFile.ToString());
    }

    [Theory]
    [AutoData]
    public void
        EnumerateFileSystemEntries_WithSearchPattern_ShouldReturnMatchingFileSystemEntries(
            string path)
    {
        IFileSystem.IFileInfo foundFile1 = FileSystem.GenerateRandomFile(path);
        IFileSystem.IFileInfo foundFile2 = FileSystem.GenerateRandomFile(path);
        IFileSystem.IFileInfo notFoundFile =
            FileSystem.GenerateRandomFileInRandomSubdirectoryOf(path);

        List<string> result = FileSystem.Directory
           .EnumerateFileSystemEntries(path, foundFile1.Name)
           .ToList();

        result.Count.Should().Be(1);
        result.Should().Contain(foundFile1.ToString());
        result.Should().NotContain(foundFile2.ToString());
        result.Should().NotContain(notFoundFile.ToString());
    }

    [Fact]
    public void
        EnumerateFileSystemEntries_WithSearchPatternInSubdirectory_ShouldReturnMatchingFileSystemEntriesInSubdirectories()
    {
        IFileSystem.IFileInfo foundFile1 = FileSystem
           .GenerateRandomFileInRandomSubdirectoryOf(fileName: "foobar");
        IFileSystem.IFileInfo foundFile2 = FileSystem
           .GenerateRandomFileInRandomSubdirectoryOf(fileName: "foobar");
        _ = FileSystem.GenerateRandomFileInRandomSubdirectoryOf();

        IEnumerable<string> result = FileSystem.Directory
           .EnumerateFileSystemEntries(".", "foobar-*.*", SearchOption.AllDirectories)
           .ToArray();

        result.Count().Should().Be(2);
        result.Should().Contain(foundFile1.ToString());
        result.Should().Contain(foundFile2.ToString());
    }
}