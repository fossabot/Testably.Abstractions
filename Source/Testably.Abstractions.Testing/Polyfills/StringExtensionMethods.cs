﻿#if NETSTANDARD2_0
using System.Diagnostics.CodeAnalysis;

namespace Testably.Abstractions.Polyfills;

/// <summary>
///     Provides extension methods to simplify writing platform independent tests.
/// </summary>
[ExcludeFromCodeCoverage]
internal static class StringExtensionMethods
{
	/// <summary>
	///     Determines whether the end of this string instance matches the specified character.
	/// </summary>
	internal static bool EndsWith(
		this string @this,
		char value)
	{
		return @this.EndsWith($"{value}");
	}
	
	/// <summary>
	///     Determines whether this string instance starts with the specified character.
	/// </summary>
	internal static bool StartsWith(
		this string @this,
		char value)
	{
		return @this.StartsWith($"{value}");
	}
}
#endif
