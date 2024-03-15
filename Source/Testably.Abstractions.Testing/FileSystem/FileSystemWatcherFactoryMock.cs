﻿using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using Testably.Abstractions.Testing.Helpers;
using Testably.Abstractions.Testing.Statistics;

namespace Testably.Abstractions.Testing.FileSystem;

internal sealed class FileSystemWatcherFactoryMock
	: IFileSystemWatcherFactory
{
	private readonly MockFileSystem _fileSystem;

	internal FileSystemWatcherFactoryMock(MockFileSystem fileSystem)
	{
		_fileSystem = fileSystem;
	}

	#region IFileSystemWatcherFactory Members

	/// <inheritdoc cref="IFileSystemEntity.FileSystem" />
	public IFileSystem FileSystem
		=> _fileSystem;

	/// <inheritdoc cref="IFileSystemWatcherFactory.CreateNew()" />
	[Obsolete("Use `IFileSystemWatcherFactory.New()` instead")]
	public IFileSystemWatcher CreateNew()
		=> New();

	/// <inheritdoc cref="IFileSystemWatcherFactory.CreateNew(string)" />
	[Obsolete("Use `IFileSystemWatcherFactory.New(string)` instead")]
	public IFileSystemWatcher CreateNew(string path)
		=> New(path);

	/// <inheritdoc cref="IFileSystemWatcherFactory.CreateNew(string, string)" />
	[Obsolete("Use `IFileSystemWatcherFactory.New(string, string)` instead")]
	public IFileSystemWatcher CreateNew(string path, string filter)
		=> New(path, filter);

	/// <inheritdoc cref="IFileSystemWatcherFactory.New()" />
	public IFileSystemWatcher New()
	{
		using IDisposable registration = RegisterMethod(nameof(New));

		return FileSystemWatcherMock.New(_fileSystem);
	}

	/// <inheritdoc cref="IFileSystemWatcherFactory.New(string)" />
	public IFileSystemWatcher New(string path)
	{
		using IDisposable registration = RegisterMethod(nameof(New),
			path);

		FileSystemWatcherMock fileSystemWatcherMock =
			FileSystemWatcherMock.New(_fileSystem);
		fileSystemWatcherMock.Path = path.EnsureValidArgument(_fileSystem);
		return fileSystemWatcherMock;
	}

	/// <inheritdoc cref="IFileSystemWatcherFactory.New(string, string)" />
	public IFileSystemWatcher New(string path, string filter)
	{
		using IDisposable registration = RegisterMethod(nameof(New),
			path, filter);

		FileSystemWatcherMock fileSystemWatcherMock =
			FileSystemWatcherMock.New(_fileSystem);
		fileSystemWatcherMock.Path = path.EnsureValidArgument(_fileSystem);
		fileSystemWatcherMock.Filter = filter;
		return fileSystemWatcherMock;
	}

	/// <inheritdoc cref="IFileSystemWatcherFactory.Wrap(FileSystemWatcher)" />
	[return: NotNullIfNotNull("fileSystemWatcher")]
	// ReSharper disable once ReturnTypeCanBeNotNullable
	public IFileSystemWatcher? Wrap(FileSystemWatcher? fileSystemWatcher)
	{
		using IDisposable registration = RegisterMethod(nameof(Wrap),
			fileSystemWatcher);

		if (fileSystemWatcher == null)
		{
			return null;
		}

		FileSystemWatcherMock fileSystemWatcherMock =
			FileSystemWatcherMock.New(_fileSystem);
		fileSystemWatcherMock.Path = fileSystemWatcher.Path;
#if FEATURE_FILESYSTEMWATCHER_ADVANCED
		foreach (string filter in fileSystemWatcher.Filters)
		{
			fileSystemWatcherMock.Filters.Add(filter);
		}
#else
		fileSystemWatcherMock.Filter = fileSystemWatcher.Filter;
#endif
		fileSystemWatcherMock.NotifyFilter = fileSystemWatcher.NotifyFilter;
		fileSystemWatcherMock.IncludeSubdirectories =
			fileSystemWatcher.IncludeSubdirectories;
		fileSystemWatcherMock.InternalBufferSize =
			fileSystemWatcher.InternalBufferSize;
		fileSystemWatcherMock.EnableRaisingEvents =
			fileSystemWatcher.EnableRaisingEvents;
		return fileSystemWatcherMock;
	}

	#endregion

	private IDisposable RegisterMethod(string name)
		=> _fileSystem.StatisticsRegistration.FileSystemWatcher.RegisterMethod(name);

	private IDisposable RegisterMethod<T1>(string name, T1 parameter1)
		=> _fileSystem.StatisticsRegistration.FileSystemWatcher.RegisterMethod(name,
			ParameterDescription.FromParameter(parameter1));

	private IDisposable RegisterMethod<T1, T2>(string name, T1 parameter1, T2 parameter2)
		=> _fileSystem.StatisticsRegistration.FileSystemWatcher.RegisterMethod(name,
			ParameterDescription.FromParameter(parameter1),
			ParameterDescription.FromParameter(parameter2));
}
