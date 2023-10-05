using System.Reflection;

namespace Architect.DddEfDemo.DddEfDemo.Domain.UnitTests;

internal static class TypeExtensions
{
	private static readonly BindingFlags BindingFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

	public static bool HasDefaultConstructor(this Type type)
	{
		return type.GetConstructor(BindingFlags, Array.Empty<Type>()) is not null;
	}
}
