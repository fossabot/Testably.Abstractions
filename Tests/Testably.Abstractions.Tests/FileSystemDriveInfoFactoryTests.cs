using System.IO;
using System.Linq;

namespace Testably.Abstractions.Tests;

public abstract class FileSystemDriveInfoFactoryTests<TFileSystem>
    where TFileSystem : IFileSystem
{
    #region Test Setup

    public string BasePath { get; }

    public TFileSystem FileSystem { get; }
    public ITimeSystem TimeSystem { get; }

    protected FileSystemDriveInfoFactoryTests(
        TFileSystem fileSystem,
        ITimeSystem timeSystem,
        string basePath)
    {
        FileSystem = fileSystem;
        TimeSystem = timeSystem;
        BasePath = basePath;
    }

    #endregion

    [Fact]
    [FileSystemTests.DriveInfoFactory(nameof(IFileSystem.IDriveInfoFactory.GetDrives))]
    public void GetDrives_ShouldNotBeEmpty()
    {
        IFileSystem.IDriveInfo[] result = FileSystem.DriveInfo.GetDrives();

        result.Should().NotBeEmpty();
    }

    [SkippableTheory]
    [AutoData]
    [FileSystemTests.DriveInfoFactory(nameof(IFileSystem.IDriveInfoFactory.New))]
    public void New_InvalidDriveName_ShouldThrowArgumentNullException(
        string invalidDriveName)
    {
        Skip.IfNot(Test.RunsOnWindows, "Linux does not support different drives.");
        Exception? exception = Record.Exception(() =>
        {
            _ = FileSystem.DriveInfo.New(invalidDriveName);
        });

        exception.Should().BeOfType<ArgumentException>();
    }

    [Fact]
    [FileSystemTests.DriveInfoFactory(nameof(IFileSystem.IDriveInfoFactory.New))]
    public void New_Null_ShouldThrowArgumentNullException()
    {
        Exception? exception = Record.Exception(() =>
        {
            _ = FileSystem.DriveInfo.New(null!);
        });

        exception.Should().BeOfType<ArgumentNullException>();
    }

    [SkippableTheory]
    [InlineData('A')]
    [InlineData('C')]
    [InlineData('X')]
    [FileSystemTests.DriveInfoFactory(nameof(IFileSystem.IDriveInfoFactory.New))]
    public void New_WithDriveLetter_ShouldReturnDriveInfo(char driveLetter)
    {
        Skip.IfNot(Test.RunsOnWindows, "Linux does not support different drives.");

        IFileSystem.IDriveInfo result = FileSystem.DriveInfo.New($"{driveLetter}");

        result.Name.Should().Be($"{driveLetter}:\\");
    }

    [SkippableTheory]
    [InlineAutoData('A')]
    [InlineAutoData('C')]
    [InlineAutoData('Y')]
    [FileSystemTests.DriveInfoFactory(nameof(IFileSystem.IDriveInfoFactory.New))]
    public void New_WithRootedPath_ShouldReturnDriveInfo(char driveLetter, string path)
    {
        Skip.IfNot(Test.RunsOnWindows, "Linux does not support different drives.");

        string rootedPath = path.PrefixRoot(driveLetter);

        IFileSystem.IDriveInfo result = FileSystem.DriveInfo.New(rootedPath);

        result.Name.Should().Be($"{driveLetter}:\\");
    }

    [SkippableFact]
    [FileSystemTests.DriveInfoFactory(nameof(IFileSystem.IDriveInfoFactory.GetDrives))]
    public void Wrap_ShouldReturnDriveInfoWithSameName()
    {
        Skip.IfNot(Test.RunsOnWindows, "Linux does not support different drives.");

        DriveInfo driveInfo = new("C");
        IFileSystem.IDriveInfo result = FileSystem.DriveInfo.Wrap(driveInfo);

        result.Name.Should().Be(driveInfo.Name);
    }

    [SkippableTheory]
    [AutoData]
    [FileSystemTests.DriveInfoFactory("MissingDrives")]
    public void MissingDrive_WriteAllBytes_ShouldThrowDirectoryNotFoundException(
        string path, byte[] contents)
    {
        Skip.IfNot(Test.RunsOnWindows);

        IFileSystem.IDriveInfo driveInfo = GetUnmappedDrive();

        path = $"{driveInfo.Name}{path}";

        Exception? exception = Record.Exception(() =>
        {
            FileSystem.File.WriteAllBytes(path, contents);
        });

        exception.Should().BeOfType<DirectoryNotFoundException>()
           .Which.Message.Should().Contain($"'{path}'");
    }

    [SkippableTheory]
    [AutoData]
    [FileSystemTests.DriveInfoFactory("MissingDrives")]
    public void MissingDrive_CreateDirectoryInfo_ShouldOnlyThrowWhenAccessingData(
        string path, string subPath)
    {
        Skip.IfNot(Test.RunsOnWindows);

        IFileSystem.IDriveInfo driveInfo = GetUnmappedDrive();

        path = $"{driveInfo.Name}{path}";
        IFileSystem.IDirectoryInfo directoryInfo = FileSystem.DirectoryInfo.New(FileSystem.Path.Combine(path, subPath));
        var parent = directoryInfo.Parent;

        Exception? exception = Record.Exception(() =>
        {
            _ = parent!.EnumerateDirectories().ToArray();
        });

        exception.Should().BeOfType<DirectoryNotFoundException>()
           .Which.Message.Should().Contain($"'{path}'");
    }

    private IFileSystem.IDriveInfo GetUnmappedDrive()
    {
        IFileSystem.IDriveInfo? driveInfo = null;
        for (char c = 'A'; c <= 'Z'; c++)
        {
            driveInfo = FileSystem.DriveInfo.New($"{c}");
            if (FileSystem.DriveInfo.GetDrives().All(d => d.Name != driveInfo.Name))
            {
                break;
            }
        }

        return driveInfo ?? throw new NotSupportedException("No unmapped drive found!");
    }
}