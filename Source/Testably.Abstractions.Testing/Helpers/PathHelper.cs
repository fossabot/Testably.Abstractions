﻿using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;

namespace Testably.Abstractions.Testing.Helpers;

internal static class PathHelper
{
	private static readonly char[] AdditionalInvalidPathChars = { '*', '?' };

	internal static readonly string UncPrefix = new(Path.DirectorySeparatorChar, 2);

	internal static readonly string UncAltPrefix = new(Path.AltDirectorySeparatorChar, 2);

	/// <summary>
	///     Determines whether the given path contains illegal characters.
	/// </summary>
	internal static bool HasIllegalCharacters(this string path, IFileSystem fileSystem)
	{
		char[] invalidPathChars = fileSystem.Path.GetInvalidPathChars();

		if (path.IndexOfAny(invalidPathChars) >= 0)
		{
			return true;
		}

		return Execute.OnWindows(
			() => path.IndexOfAny(AdditionalInvalidPathChars) >= 0,
			() => false);
	}

	internal static bool IsUncPath(this string? path)
	{
		if (path == null)
		{
			return false;
		}

		return Execute.OnWindows(
			() => path.StartsWith(UncPrefix) || path.StartsWith(UncAltPrefix),
			() => path.StartsWith(UncPrefix));
	}

	internal static void ThrowCommonExceptionsIfPathIsInvalid(
		[NotNull] this string? path, IFileSystem fileSystem)
	{
		if (path == null)
		{
			throw new ArgumentNullException(nameof(path));
		}

		if (path.Length == 0)
		{
			throw ExceptionFactory.PathCannotBeEmpty();
		}
#pragma warning disable CA2249 // Consider using String.Contains with char instead of String.IndexOf not possible in .NETSTANDARD2.0
		if (path.IndexOf('\0') >= 0)
#pragma warning restore CA2249
		{
			throw ExceptionFactory.PathHasIllegalCharacters(path);
		}

		if (path.HasIllegalCharacters(fileSystem))
		{
			Execute.OnNetFramework(()
				=> throw ExceptionFactory.PathHasIllegalCharacters(path));

			throw ExceptionFactory.PathHasIncorrectSyntax(
				fileSystem.Path.GetFullPath(path));
		}
	}

	internal static string TrimOnWindows(this string path)
		=> Execute.OnWindows(
			() => path.TrimEnd(' '),
			() => path);
}