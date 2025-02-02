using System.IO;

namespace Testably.Abstractions.Tests.FileSystem.File;

// ReSharper disable once PartialTypeWithSinglePart
public abstract partial class CopyTests<TFileSystem>
	: FileSystemTestBase<TFileSystem>
	where TFileSystem : IFileSystem
{
	[SkippableTheory]
	[AutoData]
	public void
		Copy_DestinationDirectoryDoesNotExist_ShouldThrowDirectoryNotFoundException(
			string source)
	{
		FileSystem.Initialize()
			.WithFile(source);
		string destination = FileTestHelper.RootDrive(Test, "not-existing/path/foo.txt");

		Exception? exception = Record.Exception(() =>
		{
			FileSystem.File.Copy(source, destination);
		});

		exception.Should().BeException<DirectoryNotFoundException>(hResult: -2147024893);
	}

	[SkippableTheory]
	[AutoData]
	public void Copy_DestinationExists_ShouldThrowIOException_AndNotCopyFile(
		string sourceName,
		string destinationName,
		string sourceContents,
		string destinationContents)
	{
		FileSystem.File.WriteAllText(sourceName, sourceContents);
		FileSystem.File.WriteAllText(destinationName, destinationContents);

		Exception? exception = Record.Exception(() =>
		{
			FileSystem.File.Copy(sourceName, destinationName);
		});

		exception.Should().BeException<IOException>(hResult: Test.RunsOnWindows ? -2147024816 : 17);

		FileSystem.Should().HaveFile(sourceName)
			.Which.HasContent(sourceContents);
		FileSystem.Should().HaveFile(destinationName)
			.Which.HasContent(destinationContents);
	}

#if FEATURE_FILE_MOVETO_OVERWRITE
	[SkippableTheory]
	[AutoData]
	public void Copy_DestinationExists_WithOverwrite_ShouldOverwriteDestination(
		string sourceName,
		string destinationName,
		string sourceContents,
		string destinationContents)
	{
		FileSystem.File.WriteAllText(sourceName, sourceContents);
		FileSystem.File.WriteAllText(destinationName, destinationContents);

		FileSystem.File.Copy(sourceName, destinationName, true);

		FileSystem.Should().HaveFile(sourceName)
			.Which.HasContent(sourceContents);
		FileSystem.Should().HaveFile(destinationName)
			.Which.HasContent(sourceContents);
	}
#endif

	[SkippableTheory]
	[InlineData(@"0:\something\demo.txt", @"C:\elsewhere\demo.txt")]
	[InlineData(@"C:\something\demo.txt", @"^:\elsewhere\demo.txt")]
	[InlineData(@"C:\something\demo.txt", @"C:\elsewhere:\demo.txt")]
	public void
		Copy_InvalidDriveName_ShouldThrowNotSupportedException(
			string source, string destination)
	{
		Skip.IfNot(Test.IsNetFramework);

		Exception? exception = Record.Exception(() =>
		{
			FileSystem.File.Copy(source, destination);
		});

		exception.Should().BeException<NotSupportedException>(hResult: -2146233067);
	}

	[SkippableTheory]
	[InlineData(@"C::\something\demo.txt", @"C:\elsewhere\demo.txt")]
	public void
		Copy_InvalidPath_ShouldThrowCorrectException(
			string source, string destination)
	{
		Skip.IfNot(Test.RunsOnWindows);

		Exception? exception = Record.Exception(() =>
		{
			FileSystem.File.Copy(source, destination);
		});

		if (Test.IsNetFramework)
		{
			exception.Should().BeException<NotSupportedException>(hResult: -2146233067);
		}
		else
		{
			exception.Should().BeException<IOException>(hResult: -2147024773);
		}
	}

	[SkippableTheory]
	[AutoData]
	public void Copy_ReadOnly_ShouldCopyFile(
		string sourceName, string destinationName, string contents)
	{
		FileSystem.File.WriteAllText(sourceName, contents);
		FileSystem.File.SetAttributes(sourceName, FileAttributes.ReadOnly);

		FileSystem.File.Copy(sourceName, destinationName);

		FileSystem.Should().HaveFile(sourceName)
			.Which.HasContent(contents);
		FileSystem.Should().HaveFile(destinationName)
			.Which.HasContent(contents)
			.And.HasAttribute(FileAttributes.ReadOnly);
	}

	[SkippableTheory]
	[AutoData]
	public void Copy_ShouldAdjustTimes(
		string source, string destination)
	{
		SkipIfLongRunningTestsShouldBeSkipped();

		DateTime creationTimeStart = TimeSystem.DateTime.UtcNow;
		FileSystem.File.WriteAllText(source, "foo");
		DateTime creationTimeEnd = TimeSystem.DateTime.UtcNow;
		TimeSystem.Thread.Sleep(FileTestHelper.AdjustTimesDelay);
		DateTime updateTime = TimeSystem.DateTime.UtcNow;

		FileSystem.File.Copy(source, destination);

		DateTime sourceCreationTime = FileSystem.File.GetCreationTimeUtc(source);
		DateTime sourceLastAccessTime = FileSystem.File.GetLastAccessTimeUtc(source);
		DateTime sourceLastWriteTime = FileSystem.File.GetLastWriteTimeUtc(source);
		DateTime destinationCreationTime =
			FileSystem.File.GetCreationTimeUtc(destination);
		DateTime destinationLastAccessTime =
			FileSystem.File.GetLastAccessTimeUtc(destination);
		DateTime destinationLastWriteTime =
			FileSystem.File.GetLastWriteTimeUtc(destination);

		sourceCreationTime.Should()
			.BeOnOrAfter(creationTimeStart.ApplySystemClockTolerance()).And
			.BeOnOrBefore(creationTimeEnd);
		if (Test.RunsOnMac)
		{
#if NET8_0_OR_GREATER
			sourceLastAccessTime.Should()
				.BeOnOrAfter(creationTimeStart.ApplySystemClockTolerance()).And
				.BeOnOrBefore(creationTimeEnd);
#else
			sourceLastAccessTime.Should()
				.BeOnOrAfter(updateTime.ApplySystemClockTolerance());
#endif
		}
		else if (Test.RunsOnWindows)
		{
			sourceLastAccessTime.Should()
				.BeOnOrAfter(creationTimeStart.ApplySystemClockTolerance()).And
				.BeOnOrBefore(creationTimeEnd);
		}
		else
		{
			sourceLastAccessTime.Should()
				.BeOnOrAfter(updateTime.ApplySystemClockTolerance());
		}

		sourceLastWriteTime.Should()
			.BeOnOrAfter(creationTimeStart.ApplySystemClockTolerance()).And
			.BeOnOrBefore(creationTimeEnd);
		if (Test.RunsOnWindows)
		{
			destinationCreationTime.Should()
				.BeOnOrAfter(updateTime.ApplySystemClockTolerance());
		}
		else
		{
			destinationCreationTime.Should()
				.BeOnOrAfter(creationTimeStart.ApplySystemClockTolerance()).And
				.BeOnOrBefore(creationTimeEnd);
		}

		if (!Test.RunsOnMac)
		{
			destinationLastAccessTime.Should()
				.BeOnOrAfter(updateTime.ApplySystemClockTolerance());
		}

		destinationLastWriteTime.Should()
			.BeOnOrAfter(creationTimeStart.ApplySystemClockTolerance()).And
			.BeOnOrBefore(creationTimeEnd);
	}

	[SkippableTheory]
	[AutoData]
	public void Copy_ShouldCloneBinaryContent(
		string source, string destination, byte[] original)
	{
		FileSystem.File.WriteAllBytes(source, original);

		FileSystem.File.Copy(source, destination);

		using (FileSystemStream stream =
			FileSystem.File.Open(source, FileMode.Open, FileAccess.ReadWrite))
		{
			BinaryWriter binaryWriter = new(stream);

			binaryWriter.Seek(0, SeekOrigin.Begin);
			binaryWriter.Write("Some text");
		}

		FileSystem.Should().HaveFile(destination)
			.Which.HasContent(original);
		FileSystem.File.ReadAllBytes(destination).Should()
			.NotBeEquivalentTo(FileSystem.File.ReadAllBytes(source));
	}

	[SkippableTheory]
	[AutoData]
	public void Copy_ShouldCloneTextContent(
		string source, string destination, string contents)
	{
		FileSystem.File.WriteAllText(source, contents);

		FileSystem.File.Copy(source, destination);

		using (FileSystemStream stream =
			FileSystem.File.Open(source, FileMode.Open, FileAccess.ReadWrite))
		{
			BinaryWriter binaryWriter = new(stream);

			binaryWriter.Seek(0, SeekOrigin.Begin);
			binaryWriter.Write("Some text");
		}

		FileSystem.File.ReadAllText(source).Should()
			.NotBe(FileSystem.File.ReadAllText(destination));
	}

	[SkippableTheory]
	[AutoData]
	public void Copy_ShouldCopyFileWithContent(
		string sourceName, string destinationName, string contents)
	{
		SkipIfLongRunningTestsShouldBeSkipped();

		FileSystem.File.WriteAllText(sourceName, contents);

		TimeSystem.Thread.Sleep(1000);

		FileSystem.File.Copy(sourceName, destinationName);
		FileSystem.Should().HaveFile(sourceName)
			.Which.HasContent(contents);
		FileSystem.Should().HaveFile(destinationName)
			.Which.HasContent(contents);
	}

	[SkippableTheory]
	[AutoData]
	public void Copy_SourceIsDirectory_ShouldThrowUnauthorizedAccessException_AndNotCopyFile(
		string sourceName,
		string destinationName)
	{
		FileSystem.Directory.CreateDirectory(sourceName);

		Exception? exception = Record.Exception(() =>
		{
			FileSystem.File.Copy(sourceName, destinationName);
		});

		exception.Should().BeException<UnauthorizedAccessException>(
			hResult: -2147024891,
			messageContains: Test.IsNetFramework
				? $"'{sourceName}'"
				: $"'{FileSystem.Path.GetFullPath(sourceName)}'");
		FileSystem.Should().HaveDirectory(sourceName);
		FileSystem.Should().NotHaveFile(destinationName);
	}

	[SkippableTheory]
	[AutoData]
	public void Copy_SourceLocked_ShouldThrowIOException(
		string sourceName,
		string destinationName)
	{
		FileSystem.File.WriteAllText(sourceName, null);
		using FileSystemStream stream = FileSystem.File.Open(sourceName, FileMode.Open,
			FileAccess.Read, FileShare.None);

		Exception? exception = Record.Exception(() =>
		{
			FileSystem.File.Copy(sourceName, destinationName);
		});

		if (Test.RunsOnWindows)
		{
			exception.Should().BeException<IOException>(hResult: -2147024864);
			FileSystem.Should().NotHaveFile(destinationName);
		}
		else
		{
			FileSystem.Should().HaveFile(sourceName);
			FileSystem.Should().NotHaveFile(destinationName);
		}
	}

	[SkippableTheory]
	[AutoData]
	public void Copy_SourceMissing_ShouldThrowFileNotFoundException(
		string sourceName,
		string destinationName)
	{
		Exception? exception = Record.Exception(() =>
		{
			FileSystem.File.Copy(sourceName, destinationName);
		});

		exception.Should().BeException<FileNotFoundException>(
			hResult: -2147024894,
			messageContains: Test.IsNetFramework
				? null
				: $"'{FileSystem.Path.GetFullPath(sourceName)}'");
		FileSystem.Should().NotHaveFile(destinationName);
	}
}
