﻿using System;
using System.IO;
using Testably.Abstractions.FileSystem;

namespace Testably.Abstractions;

internal static class AccessControlHelpers
{
	public const string AccessControl = nameof(AccessControl);

	public static TFileSystemInfo ThrowIfMissing<TFileSystemInfo>(
		this TFileSystemInfo fileSystemInfo)
		where TFileSystemInfo : IFileSystemInfo
	{
		if (!fileSystemInfo.Exists)
		{
			if (fileSystemInfo is IDirectoryInfo directoryInfo)
			{
				throw new DirectoryNotFoundException(
					$"Could not find a part of the path '{directoryInfo.FullName}'.")
				{
#if FEATURE_EXCEPTION_HRESULT
					HResult = -2147024893
#endif
				};
			}

			if (fileSystemInfo is IFileInfo fileInfo)
			{
				throw new FileNotFoundException($"Could not find file '{fileInfo.FullName}'.")
				{
#if FEATURE_EXCEPTION_HRESULT
					HResult = -2147024894
#endif
				};
			}
		}

		return fileSystemInfo;
	}
	public static IDirectoryInfo ThrowIfParentMissing(
		this IDirectoryInfo fileSystemInfo)
	{
		if (fileSystemInfo.Parent?.Exists != true)
		{
			throw new UnauthorizedAccessException();
		}

		return fileSystemInfo;
	}
}