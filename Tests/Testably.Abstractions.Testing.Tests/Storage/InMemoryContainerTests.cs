﻿using System.IO;
using System.Linq;
using Testably.Abstractions.Testing.FileSystem;
using Testably.Abstractions.Testing.Storage;
using Testably.Abstractions.Testing.Tests.TestHelpers;

namespace Testably.Abstractions.Testing.Tests.Storage;

public class InMemoryContainerTests
{
	[Theory]
	[AutoData]
	public void AdjustAttributes_Decrypt_ShouldNotHaveEncryptedAttribute(string path)
	{
		MockFileSystem fileSystem = new();
		DriveInfoMock drive =
			DriveInfoMock.New("C", fileSystem);
		IStorageLocation location = InMemoryLocation.New(fileSystem, drive,
			fileSystem.Path.GetFullPath(path));

		InMemoryContainer container = new(FileSystemTypes.File, location, fileSystem);
		container.Encrypt();
		container.Decrypt();

		FileAttributes result = container.AdjustAttributes(FileAttributes.Normal);

		result.Should().NotHaveFlag(FileAttributes.Encrypted);
	}

	[Theory]
	[AutoData]
	public void AdjustAttributes_Encrypt_ShouldHaveEncryptedAttribute(string path)
	{
		MockFileSystem fileSystem = new();
		DriveInfoMock drive =
			DriveInfoMock.New("C", fileSystem);
		IStorageLocation location = InMemoryLocation.New(fileSystem, drive,
			fileSystem.Path.GetFullPath(path));

		InMemoryContainer container = new(FileSystemTypes.File, location, fileSystem);
		container.Encrypt();

		FileAttributes result = container.AdjustAttributes(FileAttributes.Normal);

		result.Should().HaveFlag(FileAttributes.Encrypted);
	}

	[Theory]
	[AutoData]
	public void AdjustAttributes_LeadingDot_ShouldBeHiddenOnLinux(string path)
	{
		path = "." + path;
		MockFileSystem fileSystem = new();
		DriveInfoMock drive =
			DriveInfoMock.New("C", fileSystem);
		IStorageLocation location = InMemoryLocation.New(fileSystem, drive,
			fileSystem.Path.GetFullPath(path));

		InMemoryContainer container = new(FileSystemTypes.File, location, fileSystem);

		FileAttributes result = container.AdjustAttributes(FileAttributes.Normal);

		if (Test.RunsOnLinux)
		{
			result.Should().HaveFlag(FileAttributes.Hidden);
		}
		else
		{
			result.Should().NotHaveFlag(FileAttributes.Hidden);
		}
	}

#if FEATURE_FILESYSTEM_LINK
	[Theory]
	[InlineAutoData(null, false)]
	[InlineAutoData("foo", true)]
	public void AdjustAttributes_ShouldHaveReparsePointAttributeWhenLinkTargetIsNotNull(
		string? linkTarget, bool shouldHaveReparsePoint, string path)
	{
		MockFileSystem fileSystem = new();
		DriveInfoMock drive =
			DriveInfoMock.New("C", fileSystem);
		IStorageLocation location = InMemoryLocation.New(fileSystem, drive,
			fileSystem.Path.GetFullPath(path));

		InMemoryContainer container = new(FileSystemTypes.File, location, fileSystem)
		{
			LinkTarget = linkTarget
		};

		FileAttributes result = container.AdjustAttributes(FileAttributes.Normal);

		if (shouldHaveReparsePoint)
		{
			result.Should().HaveFlag(FileAttributes.ReparsePoint);
		}
		else
		{
			result.Should().NotHaveFlag(FileAttributes.ReparsePoint);
		}
	}
#endif

	[Theory]
	[AutoData]
	public void Container_ShouldProvideCorrectTimeAndFileSystem(string path)
	{
		MockFileSystem fileSystem = new();
		IStorageLocation location = InMemoryLocation.New(fileSystem, null, path);
		IStorageContainer sut = InMemoryContainer.NewFile(location, fileSystem);

		sut.FileSystem.Should().BeSameAs(fileSystem);
		sut.TimeSystem.Should().BeSameAs(fileSystem.TimeSystem);
	}

	[Theory]
	[AutoData]
	public void Decrypt_Encrypted_ShouldDecryptBytes(
		string path, byte[] bytes)
	{
		MockFileSystem fileSystem = new();
		DriveInfoMock drive =
			DriveInfoMock.New("C", fileSystem);
		IStorageLocation location = InMemoryLocation.New(fileSystem, drive,
			fileSystem.Path.GetFullPath(path));
		IStorageContainer fileContainer = InMemoryContainer.NewFile(location, fileSystem);
		fileContainer.WriteBytes(bytes);
		fileContainer.Encrypt();

		fileContainer.Decrypt();

		fileContainer.Attributes.Should().NotHaveFlag(FileAttributes.Encrypted);
		fileContainer.GetBytes().Should().BeEquivalentTo(bytes);
	}

	[Theory]
	[AutoData]
	public void Decrypt_Unencrypted_ShouldDoNothing(
		string path, byte[] bytes)
	{
		MockFileSystem fileSystem = new();
		DriveInfoMock drive =
			DriveInfoMock.New("C", fileSystem);
		IStorageLocation location = InMemoryLocation.New(fileSystem, drive,
			fileSystem.Path.GetFullPath(path));
		IStorageContainer fileContainer = InMemoryContainer.NewFile(location, fileSystem);
		fileContainer.WriteBytes(bytes);

		fileContainer.Decrypt();

		fileContainer.GetBytes().Should().BeEquivalentTo(bytes);
	}

	[Theory]
	[AutoData]
	public void Encrypt_Encrypted_ShouldDoNothing(
		string path, byte[] bytes)
	{
		MockFileSystem fileSystem = new();
		DriveInfoMock drive =
			DriveInfoMock.New("C", fileSystem);
		IStorageLocation location = InMemoryLocation.New(fileSystem, drive,
			fileSystem.Path.GetFullPath(path));
		IStorageContainer fileContainer = InMemoryContainer.NewFile(location, fileSystem);
		fileContainer.WriteBytes(bytes);
		fileContainer.Encrypt();

		fileContainer.Encrypt();

		fileContainer.Decrypt();
		fileContainer.GetBytes().Should().BeEquivalentTo(bytes);
	}

	[Theory]
	[AutoData]
	public void Encrypt_ShouldEncryptBytes(
		string path, byte[] bytes)
	{
		MockFileSystem fileSystem = new();
		DriveInfoMock drive =
			DriveInfoMock.New("C", fileSystem);
		IStorageLocation location = InMemoryLocation.New(fileSystem, drive,
			fileSystem.Path.GetFullPath(path));
		IStorageContainer fileContainer = InMemoryContainer.NewFile(location, fileSystem);
		fileContainer.WriteBytes(bytes);

		fileContainer.Encrypt();

		fileContainer.Attributes.Should().HaveFlag(FileAttributes.Encrypted);
		fileContainer.GetBytes().Should().NotBeEquivalentTo(bytes);
	}

	[Theory]
	[AutoData]
	public void RequestAccess_ToString_DeleteAccess_ShouldContainAccessAndShare(string path,
		FileAccess access, FileShare share)
	{
		MockFileSystem fileSystem = new();
		fileSystem.Initialize()
			.WithFile(path);
		IStorageLocation location = fileSystem.Storage.GetLocation(path);
		IStorageContainer sut = InMemoryContainer.NewFile(location, fileSystem);

		IStorageAccessHandle result = sut.RequestAccess(access, share, true);

		result.ToString().Should().NotContain(access.ToString());
		result.ToString().Should().Contain("Delete");
		result.ToString().Should().Contain(share.ToString());
	}

	[Theory]
	[AutoData]
	public void RequestAccess_ToString_ShouldContainAccessAndShare(string path, FileAccess access,
		FileShare share)
	{
		MockFileSystem fileSystem = new();
		fileSystem.Initialize()
			.WithFile(path);
		IStorageLocation location = fileSystem.Storage.GetLocation(path);
		IStorageContainer sut = InMemoryContainer.NewFile(location, fileSystem);

		IStorageAccessHandle result = sut.RequestAccess(access, share);

		result.ToString().Should().Contain(access.ToString());
		result.ToString().Should().Contain(share.ToString());
	}

	[Theory]
	[AutoData]
	public void RequestAccess_WithoutDrive_ShouldThrowDirectoryNotFoundException(
		string path)
	{
		MockFileSystem fileSystem = new();
		IStorageLocation location = InMemoryLocation.New(fileSystem, null, path);
		IStorageContainer fileContainer = InMemoryContainer.NewFile(location, fileSystem);

		Exception? exception = Record.Exception(() =>
		{
			fileContainer.RequestAccess(FileAccess.Read, FileShare.Read);
		});

		exception.Should().BeOfType<DirectoryNotFoundException>();
	}

	[Theory]
	[InlineAutoData(DateTimeKind.Local)]
	[InlineAutoData(DateTimeKind.Utc)]
	public void TimeContainer_Time_Set_WithUnspecifiedKind_ShouldSetToProvidedKind(
		DateTimeKind kind, string path, DateTime time)
	{
		time = DateTime.SpecifyKind(time, DateTimeKind.Unspecified);
		MockFileSystem fileSystem = new();
		IStorageLocation location = InMemoryLocation.New(fileSystem, null, path);
		IStorageContainer fileContainer = InMemoryContainer.NewFile(location, fileSystem);

		fileContainer.CreationTime.Set(time, kind);

		DateTime result = fileContainer.CreationTime.Get(DateTimeKind.Unspecified);

		result.Should().Be(time);
		result.Kind.Should().Be(kind);
	}

	[Theory]
	[AutoData]
	public void TimeContainer_ToString_ShouldReturnUtcTime(
		string path, DateTime time)
	{
		time = DateTime.SpecifyKind(time, DateTimeKind.Local);
		string expectedString = time.ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ssZ");
		MockFileSystem fileSystem = new();
		IStorageLocation location = InMemoryLocation.New(fileSystem, null, path);
		IStorageContainer fileContainer = InMemoryContainer.NewFile(location, fileSystem);

		fileContainer.CreationTime.Set(time, DateTimeKind.Unspecified);

		string? result = fileContainer.CreationTime.ToString();

		result.Should().Be(expectedString);
	}

	[Fact]
	public void ToString_Directory_ShouldIncludePath()
	{
		MockFileSystem fileSystem = new();
		string expectedPath = fileSystem.Path.GetFullPath("foo");
		fileSystem.Directory.CreateDirectory(expectedPath);
		#pragma warning disable CA1826
		IStorageContainer sut = fileSystem.StorageContainers.Last();
		#pragma warning restore CA1826

		string? result = sut.ToString();

		result.Should().Be($"{expectedPath}: Directory");
	}

	[Theory]
	[AutoData]
	public void ToString_File_ShouldIncludePathAndFileSize(byte[] bytes)
	{
		MockFileSystem fileSystem = new();
		string expectedPath = fileSystem.Path.GetFullPath("foo.txt");
		fileSystem.File.WriteAllBytes(expectedPath, bytes);
		IStorageContainer sut = fileSystem.StorageContainers.Single();

		string? result = sut.ToString();

		result.Should().Be($"{expectedPath}: File ({bytes.Length} bytes)");
	}

	[Fact]
	public void ToString_UnknownContainer_ShouldIncludePath()
	{
		MockFileSystem fileSystem = new();
		string expectedPath = fileSystem.Path.GetFullPath("foo");
		IStorageLocation location = InMemoryLocation.New(fileSystem, null, expectedPath);
		InMemoryContainer sut = new(FileSystemTypes.DirectoryOrFile, location,
			fileSystem);

		string result = sut.ToString();

		result.Should().Be($"{expectedPath}: Unknown Container");
	}
}
