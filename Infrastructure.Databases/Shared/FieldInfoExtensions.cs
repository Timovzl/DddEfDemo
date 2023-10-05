using System.Reflection;

namespace Architect.DddEfDemo.DddEfDemo.Infrastructure.Databases.Shared;

internal static class FieldInfoExtensions
{
	/// <summary>
	/// <para>
	/// Replaces the <paramref name="field"/>'s value by reading and then writing, with automatic conversions to and from type <typeparamref name="T"/>.
	/// </para>
	/// <para>
	/// This method is primarily intended to help seed entities that contain value objects.
	/// At the time of writing, EF still treats the owned value objects as entities, requiring them to be seeded separately, with the parent entity's property nulled out.
	/// </para>
	/// </summary>
	public static T ExchangeValue<T>(this FieldInfo field, object instance, T? value)
	{
		return (T)ExchangeValue(field, instance, (object?)value)!;
	}

	/// <summary>
	/// <para>
	/// Replaces the <paramref name="field"/>'s value by reading and then writing.
	/// </para>
	/// <para>
	/// This method is primarily intended to help seed entities that contain value objects.
	/// At the time of writing, EF still treats the owned value objects as entities, requiring them to be seeded separately, with the parent entity's property nulled out.
	/// </para>
	/// </summary>
	public static object? ExchangeValue(this FieldInfo field, object instance, object? value)
	{
		var result = field.GetValue(instance);
		field.SetValue(instance, value);
		return result;
	}
}
