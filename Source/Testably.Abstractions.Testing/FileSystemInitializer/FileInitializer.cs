﻿using System;
using Testably.Abstractions.Testing.Helpers;

namespace Testably.Abstractions.Testing.FileSystemInitializer;

internal sealed class FileInitializer<TFileSystem>
	: FileSystemInitializer<TFileSystem>,
		IFileSystemFileInitializer<TFileSystem>
	where TFileSystem : IFileSystem
{
	public FileInitializer(FileSystemInitializer<TFileSystem> initializer,
		IFileInfo file)
		: base(initializer)
	{
		File = file;
	}

	#region IFileSystemFileInitializer<TFileSystem> Members

	/// <inheritdoc cref="IFileSystemFileInitializer{TFileSystem}.File" />
	public IFileInfo File { get; }

	/// <inheritdoc cref="IFileSystemFileInitializer{TFileSystem}.Which(Action{IFileManipulator})" />
	public IFileSystemFileInitializer<TFileSystem> Which(
		Action<IFileManipulator> fileManipulation)
	{
		using IDisposable release = FileSystem.IgnoreStatistics();
		FileManipulator fileManipulator = new(FileSystem, File);
		fileManipulation.Invoke(fileManipulator);
		return this;
	}

	#endregion

	private sealed class FileManipulator : IFileManipulator
	{
		internal FileManipulator(IFileSystem fileSystem, IFileInfo file)
		{
			FileSystem = fileSystem;
			File = file;
		}

		#region IFileManipulator Members

		/// <inheritdoc cref="IFileManipulator.File" />
		public IFileInfo File { get; }

		/// <inheritdoc cref="IFileSystemEntity.FileSystem" />
		public IFileSystem FileSystem { get; }

		/// <inheritdoc cref="IFileManipulator.HasBytesContent" />
		public IFileManipulator HasBytesContent(byte[] bytes)
		{
			FileSystem.File.WriteAllBytes(File.FullName, bytes);
			return this;
		}

		/// <inheritdoc cref="IFileManipulator.HasStringContent" />
		public IFileManipulator HasStringContent(string contents)
		{
			FileSystem.File.WriteAllText(File.FullName, contents);
			return this;
		}

		#endregion
	}
}
