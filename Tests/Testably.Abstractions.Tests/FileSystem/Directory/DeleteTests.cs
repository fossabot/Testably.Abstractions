using System.IO;

namespace Testably.Abstractions.Tests.FileSystem.Directory;

// ReSharper disable once PartialTypeWithSinglePart
public abstract partial class DeleteTests<TFileSystem>
	: FileSystemTestBase<TFileSystem>
	where TFileSystem : IFileSystem
{
	[SkippableTheory]
	[AutoData]
	public void
		Delete_CaseDifferentPath_ShouldThrowDirectoryNotFoundException_OnLinux(
			string directoryName)
	{
		directoryName = directoryName.ToLowerInvariant();
		FileSystem.Directory.CreateDirectory(directoryName.ToUpperInvariant());
		string expectedPath = System.IO.Path.Combine(BasePath, directoryName);
		Exception? exception = Record.Exception(() =>
		{
			FileSystem.Directory.Delete(directoryName);
		});

		if (Test.RunsOnLinux)
		{
			exception.Should().BeException<DirectoryNotFoundException>($"'{expectedPath}'",
				hResult: -2147024893);
		}
		else
		{
			exception.Should().BeNull();
			FileSystem.Should().NotHaveDirectory(directoryName.ToUpperInvariant());
		}
	}

	[SkippableTheory]
	[AutoData]
	public void Delete_FullPath_ShouldDeleteDirectory(string directoryName)
	{
		IDirectoryInfo result =
			FileSystem.Directory.CreateDirectory(directoryName);

		FileSystem.Directory.Delete(result.FullName);

		FileSystem.Should().NotHaveDirectory(directoryName);
		result.Should().NotExist();
	}

	[SkippableTheory]
	[AutoData]
	public void Delete_MissingDirectory_ShouldThrowDirectoryNotFoundException(
		string directoryName)
	{
		string expectedPath = System.IO.Path.Combine(BasePath, directoryName);
		Exception? exception = Record.Exception(() =>
		{
			FileSystem.Directory.Delete(directoryName);
		});

		exception.Should().BeException<DirectoryNotFoundException>($"'{expectedPath}'",
			hResult: -2147024893);
	}

	[SkippableTheory]
	[AutoData]
	public void Delete_Recursive_MissingDirectory_ShouldThrowDirectoryNotFoundException(
		string directoryName)
	{
		string expectedPath = System.IO.Path.Combine(BasePath, directoryName);
		Exception? exception = Record.Exception(() =>
		{
			FileSystem.Directory.Delete(directoryName, true);
		});

		exception.Should().BeException<DirectoryNotFoundException>($"'{expectedPath}'",
			hResult: -2147024893);
	}

	[SkippableTheory]
	[AutoData]
	public void Delete_Recursive_WithFileInSubdirectory_ShouldDeleteDirectoryWithContent(
		string path, string subdirectory, string fileName, string fileContent)
	{
		FileSystem.Directory.CreateDirectory(path);
		string filePath = FileSystem.Path.Combine(path, fileName);
		FileSystem.File.WriteAllText(filePath, fileContent);

		string subdirectoryPath = FileSystem.Path.Combine(path, subdirectory);
		FileSystem.Directory.CreateDirectory(subdirectoryPath);
		string subdirectoryFilePath = FileSystem.Path.Combine(path, subdirectory, fileName);
		FileSystem.File.WriteAllText(subdirectoryFilePath, fileContent);

		FileSystem.Should().HaveDirectory(path);

		FileSystem.Directory.Delete(path, true);

		FileSystem.Should().NotHaveDirectory(path);
		FileSystem.Should().NotHaveFile(filePath);
		FileSystem.Should().NotHaveDirectory(subdirectoryPath);
		FileSystem.Should().NotHaveFile(subdirectoryFilePath);
	}

	[SkippableTheory]
	[AutoData]
	public void Delete_Recursive_WithOpenFile_ShouldThrowIOException_OnWindows(
		string path, string filename)
	{
		FileSystem.Initialize()
			.WithSubdirectory(path);
		string filePath = FileSystem.Path.Combine(path, filename);
		FileSystemStream openFile = FileSystem.File.OpenWrite(filePath);
		openFile.Write(new byte[]
		{
			0
		}, 0, 1);
		openFile.Flush();
		Exception? exception = Record.Exception(() =>
		{
			FileSystem.Directory.Delete(path, true);
			openFile.Write(new byte[]
			{
				0
			}, 0, 1);
			openFile.Flush();
		});

		if (Test.RunsOnWindows)
		{
			exception.Should().BeException<IOException>($"{filename}'",
				hResult: -2147024864);
			FileSystem.Should().HaveFile(filePath);
		}
		else
		{
			exception.Should().BeNull();
			FileSystem.Should().NotHaveFile(filePath);
		}
	}

	[SkippableTheory]
	[AutoData]
	public void
		Delete_Recursive_WithSimilarNamedFile_ShouldOnlyDeleteDirectoryAndItsContents(
			string subdirectory)
	{
		string fileName = $"{subdirectory}.txt";
		FileSystem.Initialize()
			.WithSubdirectory(subdirectory).Initialized(s => s
				.WithAFile()
				.WithASubdirectory())
			.WithFile(fileName);

		FileSystem.Directory.Delete(subdirectory, true);

		FileSystem.Should().NotHaveDirectory(subdirectory);
		FileSystem.Should().HaveFile(fileName);
	}

	[SkippableTheory]
	[AutoData]
	public void Delete_Recursive_WithSubdirectory_ShouldDeleteDirectoryWithContent(
		string path, string subdirectory)
	{
		string subdirectoryPath = FileSystem.Path.Combine(path, subdirectory);
		FileSystem.Directory.CreateDirectory(subdirectoryPath);
		FileSystem.Should().HaveDirectory(path);

		FileSystem.Directory.Delete(path, true);

		FileSystem.Should().NotHaveDirectory(path);
		FileSystem.Should().NotHaveDirectory(subdirectoryPath);
	}

	[SkippableTheory]
	[AutoData]
	public void Delete_ShouldAdjustTimes(string path, string subdirectoryName)
	{
		SkipIfLongRunningTestsShouldBeSkipped();

		string subdirectoryPath = FileSystem.Path.Combine(path, subdirectoryName);
		DateTime creationTimeStart = TimeSystem.DateTime.UtcNow;
		FileSystem.Directory.CreateDirectory(subdirectoryPath);
		DateTime creationTimeEnd = TimeSystem.DateTime.UtcNow;
		TimeSystem.Thread.Sleep(FileTestHelper.AdjustTimesDelay);
		DateTime updateTime = TimeSystem.DateTime.UtcNow;

		FileSystem.Directory.Delete(subdirectoryPath);

		DateTime creationTime = FileSystem.Directory.GetCreationTimeUtc(path);
		DateTime lastAccessTime = FileSystem.Directory.GetLastAccessTimeUtc(path);
		DateTime lastWriteTime = FileSystem.Directory.GetLastWriteTimeUtc(path);

		if (Test.RunsOnWindows)
		{
			creationTime.Should()
				.BeOnOrAfter(creationTimeStart.ApplySystemClockTolerance()).And
				.BeOnOrBefore(creationTimeEnd);
			lastAccessTime.Should()
				.BeOnOrAfter(updateTime.ApplySystemClockTolerance());
		}
		else
		{
			lastAccessTime.Should()
				.BeOnOrAfter(creationTimeStart.ApplySystemClockTolerance()).And
				.BeOnOrBefore(creationTimeEnd);
		}

		lastWriteTime.Should()
			.BeOnOrAfter(updateTime.ApplySystemClockTolerance());
	}

	[SkippableTheory]
	[AutoData]
	public void Delete_ShouldDeleteDirectory(string directoryName)
	{
		IDirectoryInfo result =
			FileSystem.Directory.CreateDirectory(directoryName);

		FileSystem.Directory.Delete(directoryName);

		FileSystem.Should().NotHaveDirectory(directoryName);
		result.Should().NotExist();
	}

	[SkippableTheory]
	[AutoData]
	public void Delete_WithSimilarNamedFile_ShouldOnlyDeleteDirectory(
		string subdirectory)
	{
		string fileName = $"{subdirectory}.txt";
		FileSystem.Initialize()
			.WithSubdirectory(subdirectory)
			.WithFile(fileName);

		FileSystem.Directory.Delete(subdirectory);

		FileSystem.Should().NotHaveDirectory(subdirectory);
		FileSystem.Should().HaveFile(fileName);
	}

	[SkippableTheory]
	[AutoData]
	public void Delete_WithSubdirectory_ShouldThrowIOException_AndNotDeleteDirectory(
		string path, string subdirectory)
	{
		FileSystem.Directory.CreateDirectory(FileSystem.Path.Combine(path, subdirectory));
		FileSystem.Should().HaveDirectory(path);

		Exception? exception = Record.Exception(() =>
		{
			FileSystem.Directory.Delete(path);
		});

		exception.Should().BeException<IOException>(
			hResult: Test.DependsOnOS(windows: -2147024751, macOS: 66, linux: 39),
			// Path information only included in exception message on Windows and not in .NET Framework
			messageContains: !Test.RunsOnWindows || Test.IsNetFramework
				? null
				: $"'{System.IO.Path.Combine(BasePath, path)}'");
		FileSystem.Should().HaveDirectory(path);
	}
}
