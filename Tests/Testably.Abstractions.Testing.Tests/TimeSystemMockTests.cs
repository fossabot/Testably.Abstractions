﻿using System.Threading;
using System.Threading.Tasks;

namespace Testably.Abstractions.Testing.Tests;

public class TimeSystemMockTests
{
	[Fact]
	[Trait(nameof(Testing), nameof(TimeSystemMock))]
	public async Task Delay_Infinite_ShouldNotThrowException()
	{
		TimeSystemMock timeSystem = new();
		Exception? exception =
			await Record.ExceptionAsync(() => timeSystem.Task.Delay(Timeout.Infinite));

		exception.Should().BeNull();
	}

	[Fact]
	[Trait(nameof(Testing), nameof(TimeSystemMock))]
	public async Task Delay_InfiniteTimeSpan_ShouldNotThrowException()
	{
		TimeSystemMock timeSystem = new();
		Exception? exception =
			await Record.ExceptionAsync(()
				=> timeSystem.Task.Delay(Timeout.InfiniteTimeSpan));

		exception.Should().BeNull();
	}

	[Fact]
	[Trait(nameof(Testing), nameof(TimeSystemMock))]
	public async Task Delay_LessThanInfinite_ShouldNotThrowException()
	{
		TimeSystemMock timeSystem = new();
		Exception? exception =
			await Record.ExceptionAsync(() => timeSystem.Task.Delay(-2));

		exception.Should().BeOfType<ArgumentOutOfRangeException>();
	}

	[Fact]
	[Trait(nameof(Testing), nameof(TimeSystemMock))]
	public async Task Delay_LessThanInfiniteTimeSpan_ShouldNotThrowException()
	{
		TimeSystemMock timeSystem = new();
		Exception? exception =
			await Record.ExceptionAsync(()
				=> timeSystem.Task.Delay(TimeSpan.FromMilliseconds(-2)));

		exception.Should().BeOfType<ArgumentOutOfRangeException>();
	}

	[Fact]
	[Trait(nameof(Testing), nameof(TimeSystemMock))]
	public void Sleep_Infinite_ShouldNotThrowException()
	{
		TimeSystemMock timeSystem = new();
		Exception? exception =
			Record.Exception(() => timeSystem.Thread.Sleep(Timeout.Infinite));

		exception.Should().BeNull();
	}

	[Fact]
	[Trait(nameof(Testing), nameof(TimeSystemMock))]
	public void Sleep_InfiniteTimeSpan_ShouldNotThrowException()
	{
		TimeSystemMock timeSystem = new();
		Exception? exception =
			Record.Exception(() => timeSystem.Thread.Sleep(Timeout.InfiniteTimeSpan));

		exception.Should().BeNull();
	}

	[Fact]
	[Trait(nameof(Testing), nameof(TimeSystemMock))]
	public void Sleep_LessThanInfinite_ShouldNotThrowException()
	{
		TimeSystemMock timeSystem = new();
		Exception? exception =
			Record.Exception(() => timeSystem.Thread.Sleep(-2));

		exception.Should().BeOfType<ArgumentOutOfRangeException>();
	}

	[Fact]
	[Trait(nameof(Testing), nameof(TimeSystemMock))]
	public void Sleep_LessThanInfiniteTimeSpan_ShouldNotThrowException()
	{
		TimeSystemMock timeSystem = new();
		Exception? exception =
			Record.Exception(()
				=> timeSystem.Thread.Sleep(TimeSpan.FromMilliseconds(-2)));

		exception.Should().BeOfType<ArgumentOutOfRangeException>();
	}
}