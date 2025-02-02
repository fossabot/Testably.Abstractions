﻿using Microsoft.Win32.SafeHandles;
using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using Testably.Abstractions.Testing.Helpers;
using Testably.Abstractions.Testing.Statistics;

namespace Testably.Abstractions.Testing.FileSystem;

internal sealed class FileStreamFactoryMock : IFileStreamFactory
{
	internal const FileShare DefaultShare = FileShare.Read;
	private const int DefaultBufferSize = 4096;
	private const bool DefaultUseAsync = false;
	private readonly MockFileSystem _fileSystem;

	internal FileStreamFactoryMock(MockFileSystem fileSystem)
	{
		_fileSystem = fileSystem;
	}

	#region IFileStreamFactory Members

	/// <inheritdoc cref="IFileSystemEntity.FileSystem" />
	public IFileSystem FileSystem
		=> _fileSystem;

	/// <inheritdoc cref="IFileStreamFactory.New(string, FileMode)" />
	public FileSystemStream New(string path, FileMode mode)
	{
		using IDisposable registration = RegisterMethod(nameof(New),
			path, mode);

		return New(path,
			mode,
			mode == FileMode.Append ? FileAccess.Write : FileAccess.ReadWrite,
			DefaultShare,
			DefaultBufferSize,
			DefaultUseAsync);
	}

	/// <inheritdoc cref="IFileStreamFactory.New(string, FileMode, FileAccess)" />
	public FileSystemStream New(string path, FileMode mode, FileAccess access)
	{
		using IDisposable registration = RegisterMethod(nameof(New),
			path, mode, access);

		return New(path, mode, access, DefaultShare, DefaultBufferSize, DefaultUseAsync);
	}

	/// <inheritdoc cref="IFileStreamFactory.New(string, FileMode, FileAccess, FileShare)" />
	public FileSystemStream New(string path,
		FileMode mode,
		FileAccess access,
		FileShare share)
	{
		using IDisposable registration = RegisterMethod(nameof(New),
			path, mode, access, share);

		return New(path, mode, access, share, DefaultBufferSize, DefaultUseAsync);
	}

	/// <inheritdoc cref="IFileStreamFactory.New(string, FileMode, FileAccess, FileShare, int)" />
	public FileSystemStream New(string path,
		FileMode mode,
		FileAccess access,
		FileShare share,
		int bufferSize)
	{
		using IDisposable registration = RegisterMethod(nameof(New),
			path, mode, access, share, bufferSize);

		return New(path, mode, access, share, bufferSize, DefaultUseAsync);
	}

	/// <inheritdoc cref="IFileStreamFactory.New(string, FileMode, FileAccess, FileShare, int, bool)" />
	public FileSystemStream New(string path,
		FileMode mode,
		FileAccess access,
		FileShare share,
		int bufferSize,
		bool useAsync)
	{
		using IDisposable registration = RegisterMethod(nameof(New),
			path, mode, access, share, bufferSize, useAsync);

		return New(path,
			mode,
			access,
			share,
			bufferSize,
			useAsync ? FileOptions.Asynchronous : FileOptions.None);
	}

	/// <inheritdoc cref="IFileStreamFactory.New(string, FileMode, FileAccess, FileShare, int, FileOptions)" />
	public FileSystemStream New(string path,
		FileMode mode,
		FileAccess access,
		FileShare share,
		int bufferSize,
		FileOptions options)
	{
		using IDisposable registration = RegisterMethod(nameof(New),
			path, mode, access, share, bufferSize, options);

		return new FileStreamMock(_fileSystem,
			path,
			mode,
			access,
			share,
			bufferSize,
			options);
	}

	/// <inheritdoc cref="IFileStreamFactory.New(SafeFileHandle, FileAccess)" />
#if NET6_0_OR_GREATER
	[ExcludeFromCodeCoverage(Justification = "SafeFileHandle cannot be unit tested.")]
#endif
	public FileSystemStream New(SafeFileHandle handle, FileAccess access)
	{
		using IDisposable registration = RegisterMethod(nameof(New),
			handle, access);

		SafeFileHandleMock safeFileHandleMock = _fileSystem
			.SafeFileHandleStrategy.MapSafeFileHandle(handle);
		return New(
			safeFileHandleMock.Path,
			safeFileHandleMock.Mode,
			access,
			safeFileHandleMock.Share);
	}

	/// <inheritdoc cref="IFileStreamFactory.New(SafeFileHandle, FileAccess, int)" />
#if NET6_0_OR_GREATER
	[ExcludeFromCodeCoverage(Justification = "SafeFileHandle cannot be unit tested.")]
#endif
	public FileSystemStream New(SafeFileHandle handle, FileAccess access, int bufferSize)
	{
		using IDisposable registration = RegisterMethod(nameof(New),
			handle, access, bufferSize);

		SafeFileHandleMock safeFileHandleMock = _fileSystem
			.SafeFileHandleStrategy.MapSafeFileHandle(handle);
		return New(
			safeFileHandleMock.Path,
			safeFileHandleMock.Mode,
			access,
			safeFileHandleMock.Share,
			bufferSize);
	}

	/// <inheritdoc cref="IFileStreamFactory.New(SafeFileHandle, FileAccess, int, bool)" />
#if NET6_0_OR_GREATER
	[ExcludeFromCodeCoverage(Justification = "SafeFileHandle cannot be unit tested.")]
#endif
	public FileSystemStream New(SafeFileHandle handle, FileAccess access, int bufferSize,
		bool isAsync)
	{
		using IDisposable registration = RegisterMethod(nameof(New),
			handle, access, bufferSize, isAsync);

		SafeFileHandleMock safeFileHandleMock = _fileSystem
			.SafeFileHandleStrategy.MapSafeFileHandle(handle);
		return New(
			safeFileHandleMock.Path,
			safeFileHandleMock.Mode,
			access,
			safeFileHandleMock.Share,
			bufferSize,
			isAsync);
	}

#if FEATURE_FILESYSTEM_STREAM_OPTIONS
	/// <inheritdoc cref="IFileStreamFactory.New(string, FileStreamOptions)" />
	public FileSystemStream New(string path, FileStreamOptions options)
	{
		using IDisposable registration = RegisterMethod(nameof(New),
			path, options);

		return New(path,
			options.Mode,
			options.Access,
			options.Share,
			options.BufferSize,
			options.Options);
	}
#endif

	/// <inheritdoc cref="IFileStreamFactory.Wrap(FileStream)" />
	public FileSystemStream Wrap(FileStream fileStream)
	{
		RegisterMethod(nameof(Wrap), fileStream);
		throw ExceptionFactory.NotSupportedFileStreamWrapping();
	}

	#endregion

	private void RegisterMethod<T1>(string name, T1 parameter1)
		=> _fileSystem.StatisticsRegistration.FileStream.RegisterMethod(name,
			ParameterDescription.FromParameter(parameter1));

	private IDisposable RegisterMethod<T1, T2>(string name, T1 parameter1, T2 parameter2)
		=> _fileSystem.StatisticsRegistration.FileStream.RegisterMethod(name,
			ParameterDescription.FromParameter(parameter1),
			ParameterDescription.FromParameter(parameter2));

	private IDisposable RegisterMethod<T1, T2, T3>(string name, T1 parameter1, T2 parameter2,
		T3 parameter3)
		=> _fileSystem.StatisticsRegistration.FileStream.RegisterMethod(name,
			ParameterDescription.FromParameter(parameter1),
			ParameterDescription.FromParameter(parameter2),
			ParameterDescription.FromParameter(parameter3));

	private IDisposable RegisterMethod<T1, T2, T3, T4>(string name, T1 parameter1, T2 parameter2,
		T3 parameter3, T4 parameter4)
		=> _fileSystem.StatisticsRegistration.FileStream.RegisterMethod(name,
			ParameterDescription.FromParameter(parameter1),
			ParameterDescription.FromParameter(parameter2),
			ParameterDescription.FromParameter(parameter3),
			ParameterDescription.FromParameter(parameter4));

	private IDisposable RegisterMethod<T1, T2, T3, T4, T5>(string name, T1 parameter1,
		T2 parameter2, T3 parameter3, T4 parameter4, T5 parameter5)
		=> _fileSystem.StatisticsRegistration.FileStream.RegisterMethod(name,
			ParameterDescription.FromParameter(parameter1),
			ParameterDescription.FromParameter(parameter2),
			ParameterDescription.FromParameter(parameter3),
			ParameterDescription.FromParameter(parameter4),
			ParameterDescription.FromParameter(parameter5));

	private IDisposable RegisterMethod<T1, T2, T3, T4, T5, T6>(string name, T1 parameter1,
		T2 parameter2, T3 parameter3, T4 parameter4, T5 parameter5, T6 parameter6)
		=> _fileSystem.StatisticsRegistration.FileStream.RegisterMethod(name,
			ParameterDescription.FromParameter(parameter1),
			ParameterDescription.FromParameter(parameter2),
			ParameterDescription.FromParameter(parameter3),
			ParameterDescription.FromParameter(parameter4),
			ParameterDescription.FromParameter(parameter5),
			ParameterDescription.FromParameter(parameter6));
}
