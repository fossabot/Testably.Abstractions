using System.IO;
using System.Text;

namespace Testably.Abstractions.Tests;

public abstract partial class FileSystemFileTests<TFileSystem>
    where TFileSystem : IFileSystem
{
    [Theory]
    [AutoData]
    public void ReadAllText_MissingFile_ShouldThrowFileNotFoundException(string path)
    {
        Exception? exception = Record.Exception(() =>
        {
            FileSystem.File.ReadAllText(path);
        });

        exception.Should().BeOfType<FileNotFoundException>()
           .Which.Message.Should()
           .Be($"Could not find file '{FileSystem.Path.GetFullPath(path)}'.");
    }

    [Theory]
    [MemberAutoData(nameof(GetEncodingDifference))]
    public void ReadAllText_WithDifferentEncoding_ShouldNotReturnWrittenText(
        string contents, Encoding writeEncoding, Encoding readEncoding, string path)
    {
        FileSystem.File.WriteAllText(path, contents, writeEncoding);

        string result = FileSystem.File.ReadAllText(path, readEncoding);

        result.Should().NotBe(contents,
            $"{contents} should be different when encoding from {writeEncoding} to {readEncoding}.");
    }
}