﻿using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Xunit.Abstractions;

namespace Testably.Abstractions.Parity.Tests.TestHelpers;

public class ParityCheck
{
	public List<Type> ExcludedBaseTypes { get; } = new()
	{
		typeof(object),
		typeof(MarshalByRefObject)
	};

	public List<ConstructorInfo?> MissingConstructors { get; } = new();

	public List<FieldInfo?> MissingFields { get; } = new();

	public List<MethodInfo?> MissingMethods { get; } = new();
	public List<PropertyInfo?> MissingProperties { get; } = new();

	public ParityCheck(Type[]? excludeBaseTypes = null,
		FieldInfo?[]? excludeFields = null,
		MethodInfo?[]? excludeMethods = null,
		PropertyInfo?[]? excludeProperties = null,
		ConstructorInfo?[]? excludeConstructors = null)
	{
		if (excludeBaseTypes != null)
		{
			ExcludedBaseTypes.AddRange(excludeBaseTypes);
		}

		if (excludeFields != null)
		{
			MissingFields.AddRange(excludeFields);
		}

		if (excludeMethods != null)
		{
			MissingMethods.AddRange(excludeMethods);
		}

		if (excludeProperties != null)
		{
			MissingProperties.AddRange(excludeProperties);
		}

		if (excludeConstructors != null)
		{
			MissingConstructors.AddRange(excludeConstructors);
		}
	}

	public List<string> GetErrorsToExtensionMethods<TAbstraction>(
		Type systemType, ITestOutputHelper testOutputHelper)
	{
		List<string> parityErrors = new();
		parityErrors.AddRange(GetParityErrorsBetweenExtensionMethods<TAbstraction>(
			systemType, testOutputHelper));
		return parityErrors;
	}

	public List<string> GetErrorsToInstanceType<TAbstraction>(
		Type systemType, ITestOutputHelper testOutputHelper)
	{
		List<string> parityErrors = new();
		parityErrors.AddRange(GetParityErrorsBetweenInstanceProperties<TAbstraction>(
			systemType, testOutputHelper));
		parityErrors.AddRange(GetParityErrorsBetweenInstanceMethods<TAbstraction>(
			systemType, testOutputHelper));
		return parityErrors;
	}

	public List<string> GetErrorsToInstanceType<TAbstraction, TAbstractionFactory>(
		Type systemType, ITestOutputHelper testOutputHelper)
	{
		List<string> parityErrors = new();
		parityErrors.AddRange(
			GetParityErrorsBetweenInstanceConstructors<TAbstractionFactory>(
				systemType, testOutputHelper));
		parityErrors.AddRange(GetParityErrorsBetweenInstanceProperties<TAbstraction>(
			systemType, testOutputHelper));
		parityErrors.AddRange(GetParityErrorsBetweenInstanceMethods<TAbstraction>(
			systemType, testOutputHelper));
		return parityErrors;
	}

	public List<string> GetErrorsToStaticType<TAbstraction>(
		Type systemType, ITestOutputHelper testOutputHelper)
	{
		List<string> parityErrors = new();
		parityErrors.AddRange(GetParityErrorsBetweenStaticFields<TAbstraction>(
			systemType, testOutputHelper));
		parityErrors.AddRange(GetParityErrorsBetweenStaticMethods<TAbstraction>(
			systemType, testOutputHelper));
		return parityErrors;
	}

	private IEnumerable<string> GetParityErrorsBetweenExtensionMethods<TAbstraction>(
		Type systemType, ITestOutputHelper testOutputHelper)
	{
		foreach (MethodInfo method in systemType
			.GetMethods(
				BindingFlags.Public |
				BindingFlags.Static)
			.Where(f => !MissingMethods.Contains(f))
			.Where(f => !f.IsSpecialName)
			.OrderBy(f => f.Name)
			.ThenBy(m => m.GetParameters().Length))
		{
			ParameterInfo? firstParameter = method.GetParameters().FirstOrDefault();
			if (firstParameter == null ||
			    !ParityCheckHelper.IsTypeNameEqual(firstParameter.ParameterType.Name,
				    typeof(TAbstraction).Name))
			{
				continue;
			}

			testOutputHelper.WriteLine(
				$"Check parity for static method {method.PrintMethod($"{systemType.Name}.", "this ")}");
			if (!typeof(TAbstraction)
				.ContainsEquivalentExtensionMethod(method))
			{
				yield return method.PrintMethod();
			}
		}
	}

	private IEnumerable<string> GetParityErrorsBetweenInstanceConstructors<
		TAbstractionFactory>(
		Type systemType, ITestOutputHelper testOutputHelper)
	{
		foreach (ConstructorInfo constructor in systemType
			.GetConstructors()
			.Where(c => !MissingConstructors.Contains(c))
			.OrderBy(f => f.Name)
			.ThenBy(m => m.GetParameters().Length))
		{
			testOutputHelper.WriteLine(
				$"Check parity for constructor {constructor.PrintConstructor()}");
			if (!typeof(TAbstractionFactory)
				.ContainsEquivalentMethod(constructor))
			{
				yield return constructor.PrintConstructor();
			}
		}
	}

	private IEnumerable<string> GetParityErrorsBetweenInstanceMethods<TAbstraction>(
		Type systemType, ITestOutputHelper testOutputHelper)
	{
		foreach (MethodInfo method in systemType
			.GetMethods(
				BindingFlags.Public |
				BindingFlags.Instance)
			.Where(p => p.DeclaringType == null ||
			            !ExcludedBaseTypes.Contains(p.DeclaringType))
			.Where(m => !MissingMethods.Contains(m))
			.Where(m => !m.IsSpecialName)
			.OrderBy(m => m.Name)
			.ThenBy(m => m.GetParameters().Length))
		{
			testOutputHelper.WriteLine(
				$"Check parity for method {method.PrintMethod($"{systemType.Name}.")}");
			if (!typeof(TAbstraction)
				.ContainsEquivalentMethod(method))
			{
				yield return method.PrintMethod();
			}
		}
	}

	private IEnumerable<string> GetParityErrorsBetweenInstanceProperties<TAbstraction>(
		Type systemType, ITestOutputHelper testOutputHelper)
	{
		foreach (PropertyInfo property in systemType
			.GetProperties(
				BindingFlags.Public |
				BindingFlags.Instance)
			.Where(p => p.DeclaringType == null ||
			            !ExcludedBaseTypes.Contains(p.DeclaringType))
			.Where(p => !MissingProperties.Contains(p))
			.Where(p => !p.IsSpecialName)
			.OrderBy(p => p.Name))
		{
			testOutputHelper.WriteLine(
				$"Check parity for property {property.PrintProperty($"{systemType.Name}.")}");
			if (!typeof(TAbstraction)
				.ContainsEquivalentProperty(property))
			{
				yield return property.PrintProperty();
			}
		}
	}

	private IEnumerable<string> GetParityErrorsBetweenStaticFields<TAbstraction>(
		Type systemType, ITestOutputHelper testOutputHelper)
	{
		foreach (FieldInfo field in systemType
			.GetFields(
				BindingFlags.Public |
				BindingFlags.Static)
			.Where(f => !MissingFields.Contains(f))
			.Where(f => !f.IsSpecialName)
			.OrderBy(f => f.Name))
		{
			testOutputHelper.WriteLine(
				$"Check parity for static field {field.PrintField($"{systemType.Name}.")}");
			if (!typeof(TAbstraction)
				.ContainsEquivalentProperty(field))
			{
				yield return field.PrintField();
			}
		}
	}

	private IEnumerable<string> GetParityErrorsBetweenStaticMethods<TAbstraction>(
		Type systemType, ITestOutputHelper testOutputHelper)
	{
		foreach (MethodInfo method in systemType
			.GetMethods(
				BindingFlags.Public |
				BindingFlags.Static)
			.Where(f => !MissingMethods.Contains(f))
			.Where(f => !f.IsSpecialName)
			.OrderBy(f => f.Name)
			.ThenBy(m => m.GetParameters().Length))
		{
			testOutputHelper.WriteLine(
				$"Check parity for static method {method.PrintMethod($"{systemType.Name}.")}");
			if (!typeof(TAbstraction)
				.ContainsEquivalentMethod(method))
			{
				yield return method.PrintMethod();
			}
		}
	}
}
