using System.IO;
using System.Linq;
using System.Text;

namespace Testably.Abstractions.Tests;

public class FileSystemDriveInfoMockTests
{
    #region Test Setup

    public FileSystemMock FileSystem { get; }

    public FileSystemDriveInfoMockTests()
    {
        FileSystem = new FileSystemMock();
    }

    #endregion

    [Theory]
    [AutoData]
    [FileSystemTests.DriveInfo(nameof(FileSystemMock.IDriveInfoMock.AvailableFreeSpace))]
    public void AvailableFreeSpace_ShouldBeSetTotalSize(long size)
    {
        FileSystem.WithDrive(d => d.SetTotalSize(size));

        IFileSystem.IDriveInfo drive = FileSystem.DriveInfo.GetDrives().Single();

        drive.AvailableFreeSpace.Should().Be(size);
    }

    [Theory]
    [AutoData]
    [FileSystemTests.DriveInfo(nameof(FileSystemMock.IDriveInfoMock.AvailableFreeSpace))]
    public void AvailableFreeSpace_ShouldBeReducedByWritingToFile(
        int fileSize, string path)
    {
        byte[] bytes = new byte[fileSize];
        FileSystem.WithDrive(d => d.SetTotalSize(fileSize));
        FileSystem.RandomSystem.Random.Shared.NextBytes(bytes);

        FileSystem.File.WriteAllBytes(path, bytes);

        IFileSystem.IDriveInfo drive = FileSystem.DriveInfo.GetDrives().Single();

        drive.AvailableFreeSpace.Should().Be(0);
    }

    [Theory]
    [AutoData]
    [FileSystemTests.DriveInfo(nameof(FileSystemMock.IDriveInfoMock.AvailableFreeSpace))]
    public void AvailableFreeSpace_ShouldBeReleasedWhenDeletingAFile(
        int fileSize, string path)
    {
        byte[] bytes = new byte[fileSize];
        FileSystem.WithDrive(d => d.SetTotalSize(fileSize));
        FileSystem.RandomSystem.Random.Shared.NextBytes(bytes);

        FileSystem.File.WriteAllBytes(path, bytes);
        FileSystem.File.Delete(path);

        IFileSystem.IDriveInfo drive = FileSystem.DriveInfo.GetDrives().Single();

        drive.AvailableFreeSpace.Should().Be(fileSize);
    }

    [Theory]
    [AutoData]
    [FileSystemTests.DriveInfo(nameof(FileSystemMock.IDriveInfoMock.SetDriveFormat))]
    public void SetDriveFormat_ShouldChangeDriveFormat(string driveFormat)
    {
        FileSystem.WithDrive(d => d.SetDriveFormat(driveFormat));

        IFileSystem.IDriveInfo drive = FileSystem.DriveInfo.GetDrives().Single();
        drive.DriveFormat.Should().Be(driveFormat);
    }

    [Theory]
    [AutoData]
    [FileSystemTests.DriveInfo(nameof(FileSystemMock.IDriveInfoMock.SetDriveType))]
    public void SetDriveType_ShouldChangeDriveType(DriveType driveType)
    {
        FileSystem.WithDrive(d => d.SetDriveType(driveType));

        IFileSystem.IDriveInfo drive = FileSystem.DriveInfo.GetDrives().Single();
        drive.DriveType.Should().Be(driveType);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    [FileSystemTests.DriveInfo(nameof(FileSystemMock.IDriveInfoMock.SetIsReady))]
    public void SetIsReady_ShouldChangeIsReady(bool isReady)
    {
        FileSystem.WithDrive(d => d.SetIsReady(isReady));

        IFileSystem.IDriveInfo drive = FileSystem.DriveInfo.GetDrives().Single();
        drive.IsReady.Should().Be(isReady);
    }

    [Theory]
    [AutoData]
    [FileSystemTests.DriveInfo(nameof(FileSystemMock.IDriveInfoMock.AvailableFreeSpace))]
    public void AvailableFreeSpace_ShouldBeChangedWhenAppendingToAFile(
        string fileContent1, string fileContent2, int expectedRemainingBytes,
        string path, Encoding encoding)
    {
        int fileSize1 = encoding.GetBytes(fileContent1).Length;
        int fileSize2 = encoding.GetBytes(fileContent2).Length;
        FileSystem.WithDrive(d
            => d.SetTotalSize(fileSize1 + fileSize2 + expectedRemainingBytes));
        IFileSystem.IDriveInfo drive = FileSystem.DriveInfo.GetDrives().Single();

        FileSystem.File.WriteAllText(path, fileContent1, encoding);
        drive.AvailableFreeSpace.Should().Be(expectedRemainingBytes + fileSize2);
        FileSystem.File.AppendAllText(path, fileContent2, encoding);

        drive.AvailableFreeSpace.Should().Be(expectedRemainingBytes);
    }

    [Theory]
    [AutoData]
    [FileSystemTests.DriveInfo(nameof(FileSystemMock.IDriveInfoMock.IsReady))]
    public void NotReady_AccessFile_ShouldThrowIOException(
        string path, string contents)
    {
        FileSystem.File.WriteAllText(path, contents);
        FileSystem.WithDrive(d => d.SetIsReady(false));

        Exception? exception = Record.Exception(() =>
        {
            FileSystem.File.ReadAllText(path);
        });

        exception.Should().BeOfType<IOException>();
    }

    [Theory]
    [AutoData]
    [FileSystemTests.DriveInfo(nameof(FileSystemMock.IDriveInfoMock.IsReady))]
    public void NotReady_AccessDirectory_ShouldThrowIOException(
        string path)
    {
        FileSystem.WithDrive(d => d.SetIsReady(false));

        Exception? exception = Record.Exception(() =>
        {
            FileSystem.Directory.CreateDirectory(path);
        });

        exception.Should().BeOfType<IOException>();
    }
}