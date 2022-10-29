﻿using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using Testably.Abstractions.FileSystem;

namespace Testably.Abstractions.Testing.FileSystem;

internal sealed class DirectoryInfoFactoryMock : IDirectoryInfoFactory
{
	private readonly MockFileSystem _fileSystem;

	internal DirectoryInfoFactoryMock(MockFileSystem fileSystem)
	{
		_fileSystem = fileSystem;
	}

	#region IDirectoryInfoFactory Members

	/// <inheritdoc cref="IFileSystemExtensionPoint.FileSystem" />
	public IFileSystem FileSystem
		=> _fileSystem;

	/// <inheritdoc cref="IDirectoryInfoFactory.New(string)" />
	public IDirectoryInfo New(string path)
	{
		if (path == null)
		{
			throw new ArgumentNullException(nameof(path));
		}

		return DirectoryInfoMock.New(
			_fileSystem.Storage.GetLocation(path),
			_fileSystem);
	}

	/// <inheritdoc cref="IDirectoryInfoFactory.Wrap(DirectoryInfo)" />
	[return: NotNullIfNotNull("directoryInfo")]
	public IDirectoryInfo? Wrap(DirectoryInfo? directoryInfo)
		=> DirectoryInfoMock.New(
			_fileSystem.Storage.GetLocation(
				directoryInfo?.FullName,
				directoryInfo?.ToString()),
			_fileSystem);

	#endregion
}