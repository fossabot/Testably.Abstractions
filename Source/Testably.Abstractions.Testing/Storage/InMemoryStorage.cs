﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using Testably.Abstractions.Helpers;
using Testably.Abstractions.Testing.FileSystem;
using Testably.Abstractions.Testing.Helpers;

namespace Testably.Abstractions.Testing.Storage;

/// <summary>
///     The container storing the current data of the <see cref="IFileSystem" /> in memory.
/// </summary>
internal sealed class InMemoryStorage : IStorage
{
	private readonly ConcurrentDictionary<IStorageLocation, IStorageContainer>
		_containers = new();

	private readonly ConcurrentDictionary<string, IStorageDrive> _drives =
		new(StringComparer.OrdinalIgnoreCase);

	private readonly MockFileSystem _fileSystem;

	public InMemoryStorage(MockFileSystem fileSystem)
	{
		_fileSystem = fileSystem;
		CurrentDirectory = string.Empty.PrefixRoot(_fileSystem);
		DriveInfoMock mainDrive = DriveInfoMock.New(CurrentDirectory, _fileSystem);
		_drives.TryAdd(mainDrive.GetName(), mainDrive);
		MainDrive = mainDrive;
	}

	#region IStorage Members

	/// <inheritdoc cref="IStorage.CurrentDirectory" />
	public string CurrentDirectory { get; set; }

	/// <inheritdoc cref="IStorage.MainDrive" />
	public IStorageDrive MainDrive { get; }

	/// <inheritdoc cref="IStorage.Copy(IStorageLocation, IStorageLocation, bool)" />
	public IStorageLocation? Copy(IStorageLocation source,
		IStorageLocation destination,
		bool overwrite = false)
	{
		ThrowIfParentDoesNotExist(destination, _ => ExceptionFactory.DirectoryNotFound());

		if (!_containers.TryGetValue(source,
			out IStorageContainer? sourceContainer))
		{
			return null;
		}

		if (sourceContainer.Type != FileSystemTypes.File)
		{
			throw ExceptionFactory.AccessToPathDenied(source.FullPath);
		}

		using (_ = sourceContainer.RequestAccess(FileAccess.ReadWrite, FileShare.None))
		{
			if (overwrite &&
			    _containers.TryRemove(destination,
				    out IStorageContainer? existingContainer))
			{
				existingContainer.ClearBytes();
			}

			IStorageContainer copiedContainer =
				InMemoryContainer.NewFile(destination, _fileSystem);
			if (_containers.TryAdd(destination, copiedContainer))
			{
				copiedContainer.WriteBytes(sourceContainer.GetBytes().ToArray());
				_fileSystem.Execute.OnMac(
					() => copiedContainer.LastAccessTime.Set(
						sourceContainer.LastAccessTime.Get(DateTimeKind.Local),
						DateTimeKind.Local));
#if NET8_0_OR_GREATER
				_fileSystem.Execute.OnLinux(()
					=> sourceContainer.AdjustTimes(TimeAdjustments.LastAccessTime));
#else
				_fileSystem.Execute.NotOnWindows(()
					=> sourceContainer.AdjustTimes(TimeAdjustments.LastAccessTime));
#endif

				copiedContainer.Attributes = sourceContainer.Attributes;
				_fileSystem.Execute.OnWindowsIf(sourceContainer.Type == FileSystemTypes.File,
					() => copiedContainer.Attributes |= FileAttributes.Archive);
				_fileSystem.Execute.NotOnWindows(
					() => copiedContainer.CreationTime.Set(
						sourceContainer.CreationTime.Get(DateTimeKind.Local),
						DateTimeKind.Local));
				copiedContainer.LastWriteTime.Set(
					sourceContainer.LastWriteTime.Get(DateTimeKind.Local),
					DateTimeKind.Local);
				return destination;
			}

			throw ExceptionFactory.CannotCreateFileWhenAlreadyExists(_fileSystem.Execute.IsWindows
				? -2147024816
				: 17);
		}
	}

	/// <inheritdoc cref="IStorage.DeleteContainer(IStorageLocation, bool)" />
	public bool DeleteContainer(IStorageLocation location, bool recursive = false)
	{
		if (!_containers.TryGetValue(location, out IStorageContainer? container))
		{
			IStorageLocation? parentLocation = location.GetParent();
			if (parentLocation != null &&
			    !_containers.TryGetValue(parentLocation, out _))
			{
				throw ExceptionFactory.DirectoryNotFound(parentLocation.FullPath);
			}

			return false;
		}

		if (container.Type == FileSystemTypes.Directory)
		{
			IEnumerable<IStorageLocation> children =
				EnumerateLocations(location, FileSystemTypes.DirectoryOrFile);
			if (recursive)
			{
				foreach (IStorageLocation key in children)
				{
					DeleteContainer(key, recursive);
				}
			}
			else if (children.Any())
			{
				throw ExceptionFactory.DirectoryNotEmpty(_fileSystem.Execute, location.FullPath);
			}
		}

		NotifyFilters notifyFilters =
			container.Type == FileSystemTypes.Directory
				? NotifyFilters.DirectoryName
				: NotifyFilters.FileName;
		ChangeDescription fileSystemChange =
			_fileSystem.ChangeHandler.NotifyPendingChange(WatcherChangeTypes.Deleted,
				container.Type,
				notifyFilters, location);

		using (container.RequestAccess(FileAccess.Write, FileShare.ReadWrite,
			deleteAccess: true))
		{
			if (_containers.TryRemove(location, out IStorageContainer? removed))
			{
				removed.ClearBytes();
				_fileSystem.ChangeHandler.NotifyCompletedChange(fileSystemChange);
				CheckAndAdjustParentDirectoryTimes(location);
				return true;
			}
		}

		return false;
	}

	/// <inheritdoc cref="IStorage.EnumerateLocations(IStorageLocation, FileSystemTypes, string, EnumerationOptions?)" />
	public IEnumerable<IStorageLocation> EnumerateLocations(
		IStorageLocation location,
		FileSystemTypes type,
		string searchPattern = EnumerationOptionsHelper.DefaultSearchPattern,
		EnumerationOptions? enumerationOptions = null)
	{
		ValidateExpression(searchPattern);
		if (!_containers.ContainsKey(location))
		{
			throw ExceptionFactory.DirectoryNotFound(location.FullPath);
		}

		enumerationOptions ??= EnumerationOptionsHelper.Compatible;

		string fullPath = location.FullPath;
		string fullPathWithoutTrailingSlash = fullPath;
#if NETSTANDARD2_0
		if (!fullPath.EndsWith($"{_fileSystem.Execute.Path.DirectorySeparatorChar}"))
#else
		if (!fullPath.EndsWith(_fileSystem.Execute.Path.DirectorySeparatorChar))
#endif
		{
			fullPath += _fileSystem.Execute.Path.DirectorySeparatorChar;
		}
		else if (_fileSystem.Execute.Path.GetPathRoot(fullPath) != fullPath)
		{
			fullPathWithoutTrailingSlash =
				fullPathWithoutTrailingSlash.TrimEnd(
					_fileSystem.Execute.Path.DirectorySeparatorChar);
		}

		foreach (KeyValuePair<IStorageLocation, IStorageContainer> item in _containers
			.Where(x => x.Key.FullPath.StartsWith(fullPath,
				            _fileSystem.Execute.StringComparisonMode) &&
			            !x.Key.Equals(location)))
		{
			string? parentPath =
				_fileSystem.Execute.Path.GetDirectoryName(
					item.Key.FullPath.TrimEnd(_fileSystem.Execute.Path
						.DirectorySeparatorChar));
			if (!enumerationOptions.RecurseSubdirectories &&
			    parentPath?.Equals(fullPathWithoutTrailingSlash,
				    _fileSystem.Execute.StringComparisonMode) != true)
			{
				continue;
			}

			if (!EnumerationOptionsHelper.MatchesPattern(
				_fileSystem.Execute,
				enumerationOptions,
				_fileSystem.Execute.Path.GetFileName(item.Key.FullPath),
				searchPattern))
			{
				continue;
			}

			if (type.HasFlag(item.Value.Type))
			{
				yield return item.Key;
			}
		}
	}

	/// <inheritdoc cref="IStorage.GetContainer(IStorageLocation)" />
	[return: NotNullIfNotNull("location")]
	public IStorageContainer? GetContainer(IStorageLocation? location)
	{
		if (location == null)
		{
			return null;
		}

		if (_containers.TryGetValue(location, out IStorageContainer? container))
		{
			return container;
		}

		return NullContainer.New(_fileSystem);
	}

	/// <inheritdoc cref="IStorage.GetDrive(string?)" />
	public IStorageDrive? GetDrive(string? driveName)
	{
		if (string.IsNullOrWhiteSpace(driveName))
		{
			return null;
		}

		if (!driveName.IsUncPath(_fileSystem))
		{
			driveName = _fileSystem.Execute.Path.GetPathRoot(driveName);

			if (string.IsNullOrEmpty(driveName))
			{
				return null;
			}
		}

		DriveInfoMock drive = DriveInfoMock.New(driveName, _fileSystem);
		if (_drives.TryGetValue(drive.GetName(), out IStorageDrive? d))
		{
			return d;
		}

		return null;
	}

	/// <inheritdoc cref="IStorage.GetDrives()" />
	public IEnumerable<IStorageDrive> GetDrives()
		=> _drives.Values;

	/// <inheritdoc cref="IStorage.GetLocation(string?, string?)" />
	[return: NotNullIfNotNull("path")]
	public IStorageLocation? GetLocation(string? path, string? friendlyName = null)
	{
		if (path == null)
		{
			return null;
		}

		IStorageDrive? drive = _fileSystem.Storage.GetDrive(path);
		if (drive == null &&
		    !path.IsUncPath(_fileSystem))
		{
			drive = _fileSystem.Storage.MainDrive;
		}

		return InMemoryLocation.New(_fileSystem, drive, path.GetFullPathOrWhiteSpace(_fileSystem),
			path);
	}

	/// <inheritdoc cref="IStorage.GetOrAddDrive(string)" />
	public IStorageDrive GetOrAddDrive(string driveName)
	{
		DriveInfoMock drive = DriveInfoMock.New(driveName, _fileSystem);
		return _drives.GetOrAdd(drive.GetName(), _ => drive);
	}

	/// <inheritdoc cref="IStorage.GetOrCreateContainer" />
	public IStorageContainer GetOrCreateContainer(
		IStorageLocation location,
		Func<IStorageLocation, MockFileSystem, IStorageContainer> containerGenerator,
		IFileSystemExtensibility? fileSystemExtensibility = null)
	{
		ChangeDescription? fileSystemChange = null;
		IStorageContainer container = _containers.GetOrAdd(location,
			loc =>
			{
				IStorageContainer container =
					containerGenerator.Invoke(loc, _fileSystem);
				(fileSystemExtensibility as FileSystemExtensibility)?
					.CopyMetadataTo(container.Extensibility);
				if (container.Type == FileSystemTypes.Directory)
				{
					CreateParents(_fileSystem, loc);
				}
				else
				{
					IStorageLocation? parentLocation = loc.GetParent();
					if (parentLocation is { IsRooted: false } &&
					    !_containers.ContainsKey(parentLocation))
					{
						throw ExceptionFactory.DirectoryNotFound(loc.FullPath);
					}
				}

				CheckAndAdjustParentDirectoryTimes(loc);

				using (container.RequestAccess(FileAccess.Write, FileShare.ReadWrite))
				{
					fileSystemChange = _fileSystem.ChangeHandler.NotifyPendingChange(
						WatcherChangeTypes.Created,
						container.Type,
						NotifyFilters.DirectoryName, location);
				}

				return container;
			});
		_fileSystem.ChangeHandler.NotifyCompletedChange(fileSystemChange);
		return container;
	}

	/// <inheritdoc cref="IStorage.Move(IStorageLocation, IStorageLocation, bool, bool)" />
	public IStorageLocation? Move(IStorageLocation source,
		IStorageLocation destination,
		bool overwrite = false,
		bool recursive = false)
	{
		ThrowIfParentDoesNotExist(destination, _ => ExceptionFactory.DirectoryNotFound());

		List<Rollback> rollbacks = new();
		try
		{
			return MoveInternal(source, destination, overwrite, recursive, null,
				rollbacks);
		}
		catch (Exception)
		{
			foreach (Rollback rollback in rollbacks)
			{
				rollback.Execute();
			}

			throw;
		}
	}

	/// <inheritdoc cref="IStorage.Replace(IStorageLocation, IStorageLocation, IStorageLocation?, bool)" />
	public IStorageLocation? Replace(IStorageLocation source,
		IStorageLocation destination,
		IStorageLocation? backup,
		bool ignoreMetadataErrors = false)
	{
		ThrowIfParentDoesNotExist(destination, location
			=> _fileSystem.Execute.OnWindows<IOException>(
				() => ExceptionFactory.DirectoryNotFound(location.FullPath),
				() => ExceptionFactory.FileNotFound(location.FullPath)));

		if (!_containers.TryGetValue(source,
			out IStorageContainer? sourceContainer))
		{
			return null;
		}

		if (!_containers.TryGetValue(destination,
			out IStorageContainer? destinationContainer))
		{
			return null;
		}

		if (sourceContainer.Type != FileSystemTypes.File ||
		    destinationContainer.Type != FileSystemTypes.File)
		{
			throw ExceptionFactory.AccessToPathDenied(source.FullPath);
		}

		using (_ = sourceContainer.RequestAccess(
			FileAccess.ReadWrite,
			FileShare.None,
			ignoreMetadataErrors: ignoreMetadataErrors))
		{
			using (_ = destinationContainer.RequestAccess(
				FileAccess.ReadWrite,
				FileShare.None,
				ignoreMetadataErrors: ignoreMetadataErrors))
			{
				if (_containers.TryRemove(destination,
					out IStorageContainer? existingDestinationContainer))
				{
					int destinationBytesLength =
						existingDestinationContainer.GetBytes().Length;
					destination.Drive?.ChangeUsedBytes(-1 * destinationBytesLength);
					if (backup != null &&
					    _containers.TryAdd(backup, existingDestinationContainer))
					{
						_fileSystem.Execute.OnWindowsIf(
							sourceContainer.Type == FileSystemTypes.File,
							() => existingDestinationContainer.Attributes |=
								FileAttributes.Archive);
						backup.Drive?.ChangeUsedBytes(destinationBytesLength);
					}

					if (_containers.TryRemove(source,
						out IStorageContainer? existingSourceContainer))
					{
						int sourceBytesLength = existingSourceContainer.GetBytes().Length;
						source.Drive?.ChangeUsedBytes(-1 * sourceBytesLength);
						destination.Drive?.ChangeUsedBytes(sourceBytesLength);
						_fileSystem.Execute.OnWindowsIf(
							sourceContainer.Type == FileSystemTypes.File,
							() =>
							{
								FileAttributes targetAttributes =
									existingDestinationContainer.Attributes |
									FileAttributes.Archive;
								if (existingSourceContainer.Attributes.HasFlag(FileAttributes
									.ReadOnly))
								{
									targetAttributes |= FileAttributes.ReadOnly;
								}

								existingSourceContainer.Attributes = targetAttributes;

								existingSourceContainer.CreationTime.Set(
									existingDestinationContainer.CreationTime.Get(
										DateTimeKind.Utc),
									DateTimeKind.Utc);
							});
						_containers.TryAdd(destination, existingSourceContainer);
						return destination;
					}
				}

				return null;
			}
		}
	}

#if FEATURE_FILESYSTEM_LINK
	/// <inheritdoc cref="IStorage.ResolveLinkTarget(IStorageLocation, bool)" />
	public IStorageLocation? ResolveLinkTarget(IStorageLocation location,
		bool returnFinalTarget = false)
	{
		if (_containers.TryGetValue(location,
			    out IStorageContainer? initialContainer) &&
		    initialContainer.LinkTarget != null)
		{
			IStorageLocation? nextLocation =
				_fileSystem.Storage.GetLocation(initialContainer.LinkTarget);
			if (_containers.TryGetValue(nextLocation,
				out IStorageContainer? container))
			{
				if (returnFinalTarget)
				{
					nextLocation = ResolveFinalLinkTarget(container, location);
				}

				return nextLocation;
			}

			return nextLocation;
		}

		return null;
	}
#endif

	/// <inheritdoc
	///     cref="IStorage.TryAddContainer(IStorageLocation, Func{IStorageLocation, MockFileSystem, IStorageContainer}, out IStorageContainer?)" />
	public bool TryAddContainer(
		IStorageLocation location,
		Func<IStorageLocation, MockFileSystem, IStorageContainer> containerGenerator,
		[NotNullWhen(true)] out IStorageContainer? container)
	{
		IStorageLocation? parentLocation = location.GetParent();
		if (parentLocation is { IsRooted: false } &&
		    !_containers.ContainsKey(parentLocation))
		{
			throw ExceptionFactory.DirectoryNotFound(location.FullPath);
		}

		ChangeDescription? fileSystemChange = null;

		container = _containers.GetOrAdd(
			location,
			_ =>
			{
				IStorageContainer container =
					containerGenerator(location, _fileSystem);
				using (container.RequestAccess(FileAccess.Write, FileShare.ReadWrite))
				{
					fileSystemChange = _fileSystem.ChangeHandler.NotifyPendingChange(
						WatcherChangeTypes.Created,
						container.Type,
						NotifyFilters.DirectoryName, location);
				}

				CheckAndAdjustParentDirectoryTimes(location);
				return container;
			});

		if (fileSystemChange != null)
		{
			_fileSystem.ChangeHandler.NotifyCompletedChange(fileSystemChange);
			return true;
		}

		container = null;
		return false;
	}

	#endregion

	/// <inheritdoc cref="object.ToString()" />
	public override string ToString()
		=> $"directories: {_containers.Count(x => x.Value.Type == FileSystemTypes.Directory)}, files: {_containers.Count(x => x.Value.Type == FileSystemTypes.File)}";

	/// <summary>
	///     Returns an ordered list of all stored containers.
	/// </summary>
	internal IReadOnlyList<IStorageContainer> GetContainers()
		=> _containers
			.OrderBy(x => x.Key.FullPath)
			.Select(x => x.Value)
			.ToList();

	/// <summary>
	///     Removes the drive with the given <paramref name="driveName" />.
	/// </summary>
	internal IStorageDrive? RemoveDrive(string driveName)
	{
		_drives.TryRemove(driveName, out IStorageDrive? drive);
		return drive;
	}

	private void CheckAndAdjustParentDirectoryTimes(IStorageLocation location)
	{
		IStorageContainer? parentContainer = GetContainer(location.GetParent());
		if (parentContainer != null && parentContainer is not NullContainer)
		{
			_fileSystem.Execute.NotOnWindowsIf(
				parentContainer.Attributes.HasFlag(FileAttributes.ReadOnly),
				() => throw ExceptionFactory.AccessToPathDenied(location.FullPath));
			TimeAdjustments timeAdjustment = TimeAdjustments.LastWriteTime;
			_fileSystem.Execute.OnWindows(()
				=> timeAdjustment |= TimeAdjustments.LastAccessTime);
			parentContainer.AdjustTimes(timeAdjustment);
		}
	}

	private void CreateParents(MockFileSystem fileSystem, IStorageLocation location)
	{
		List<string> parents = new();
		string? parent = fileSystem.Execute.Path.GetDirectoryName(
			location.FullPath.TrimEnd(fileSystem.Execute.Path.DirectorySeparatorChar,
				fileSystem.Execute.Path.AltDirectorySeparatorChar));
		while (!string.IsNullOrEmpty(parent))
		{
			parents.Add(parent);
			parent = fileSystem.Execute.Path.GetDirectoryName(parent);
		}

		parents.Reverse();

		List<IStorageAccessHandle> accessHandles = new();
		try
		{
			foreach (string? parentPath in parents)
			{
				ChangeDescription? fileSystemChange = null;
				IStorageLocation parentLocation =
					_fileSystem.Storage.GetLocation(parentPath);
				_ = _containers.AddOrUpdate(
					parentLocation,
					loc =>
					{
						IStorageContainer container =
							InMemoryContainer.NewDirectory(loc, _fileSystem);

						accessHandles.Add(container.RequestAccess(FileAccess.Write,
							FileShare.ReadWrite));
						fileSystemChange =
							fileSystem.ChangeHandler.NotifyPendingChange(
								WatcherChangeTypes.Created,
								container.Type,
								NotifyFilters.DirectoryName, parentLocation);
						return container;
					},
					(_, f) => f);
				fileSystem.ChangeHandler.NotifyCompletedChange(fileSystemChange);
			}
		}
		finally
		{
			foreach (IStorageAccessHandle accessHandle in accessHandles)
			{
				accessHandle.Dispose();
			}
		}
	}

	private IStorageLocation? MoveInternal(IStorageLocation source,
		IStorageLocation destination,
		bool overwrite,
		bool recursive,
		FileSystemTypes? sourceType,
		List<Rollback>? rollbacks = null)
	{
		if (!_containers.TryGetValue(source,
			out IStorageContainer? container))
		{
			return null;
		}

		if (container.Type == FileSystemTypes.Directory &&
		    source.FullPath.Equals(destination.FullPath, _fileSystem.Execute.IsNetFramework
			    ? StringComparison.OrdinalIgnoreCase
			    : StringComparison.Ordinal))
		{
			throw ExceptionFactory.MoveSourceMustBeDifferentThanDestination();
		}

		sourceType ??= container.Type;

		List<IStorageLocation> children =
			EnumerateLocations(source, FileSystemTypes.DirectoryOrFile).ToList();
		if (children.Any() && !recursive)
		{
			throw ExceptionFactory.DirectoryNotEmpty(_fileSystem.Execute, source.FullPath);
		}

		using (container.RequestAccess(FileAccess.Write, FileShare.None,
			hResult: sourceType == FileSystemTypes.Directory ? -2147024891 : -2147024864))
		{
			if (children.Any() && recursive)
			{
				foreach (IStorageLocation child in children)
				{
					IStorageLocation childDestination = _fileSystem
						.GetMoveLocation(child, source, destination);
					MoveInternal(child, childDestination, overwrite, recursive,
						sourceType,
						rollbacks: rollbacks);
				}
			}

			ChangeDescription fileSystemChange =
				_fileSystem.ChangeHandler.NotifyPendingChange(WatcherChangeTypes.Renamed,
					container.Type,
					NotifyFilters.FileName,
					destination,
					source);
			if (_containers.TryRemove(source, out IStorageContainer? sourceContainer))
			{
				if (overwrite &&
				    _containers.TryRemove(destination,
					    out IStorageContainer? existingContainer))
				{
					existingContainer.ClearBytes();
				}

				if (_containers.TryAdd(destination, sourceContainer))
				{
					int bytesLength = sourceContainer.GetBytes().Length;
					source.Drive?.ChangeUsedBytes(-1 * bytesLength);
					destination.Drive?.ChangeUsedBytes(bytesLength);
					_fileSystem.Execute.OnWindowsIf(sourceContainer.Type == FileSystemTypes.File,
						() => sourceContainer.Attributes |= FileAttributes.Archive);
					rollbacks?.Add(new Rollback(
						() => MoveInternal(destination, source, true, false,
							sourceType)));
					_fileSystem.ChangeHandler.NotifyCompletedChange(fileSystemChange);
					return destination;
				}

				_containers.TryAdd(source, sourceContainer);
				int hResult = -2147024891;
				if (sourceType != FileSystemTypes.Directory)
				{
					hResult = _fileSystem.Execute.IsWindows ? -2147024713 : 17;
				}

				throw ExceptionFactory.CannotCreateFileWhenAlreadyExists(hResult);
			}
		}

		return source;
	}

#if FEATURE_FILESYSTEM_LINK
	private IStorageLocation? ResolveFinalLinkTarget(IStorageContainer container,
		IStorageLocation originalLocation)
	{
		int maxResolveLinks = _fileSystem.Execute.IsWindows ? 63 : 40;
		IStorageLocation? nextLocation = null;
		for (int i = 1; i < maxResolveLinks; i++)
		{
			if (container.LinkTarget == null)
			{
				break;
			}

			nextLocation = _fileSystem.Storage.GetLocation(container.LinkTarget);
			if (!_containers.TryGetValue(nextLocation,
				out IStorageContainer? nextContainer))
			{
				return nextLocation;
			}

			container = nextContainer;
		}

		if (container.LinkTarget != null)
		{
			throw ExceptionFactory.FileNameCannotBeResolved(
				originalLocation.FullPath);
		}

		return nextLocation;
	}
#endif

	private void ThrowIfParentDoesNotExist(IStorageLocation location,
		Func<IStorageLocation, IOException>
			exceptionCallback)
	{
		IStorageLocation? parentLocation = location.GetParent();
		if (parentLocation != null &&
		    _fileSystem.Execute.Path.GetPathRoot(parentLocation.FullPath) !=
		    parentLocation.FullPath &&
		    !_containers.TryGetValue(parentLocation, out _))
		{
			throw exceptionCallback(parentLocation);
		}
	}

	private static void ValidateExpression(string expression)
	{
		if (expression.Contains('\0'))
		{
			throw ExceptionFactory.PathHasIllegalCharacters(expression);
		}
	}

	private sealed class Rollback
	{
		private readonly Action _onRollback;

		public Rollback(Action onRollback)
		{
			_onRollback = onRollback;
		}

		public void Execute()
		{
			_onRollback.Invoke();
		}
	}
}
