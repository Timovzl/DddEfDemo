namespace Architect.DddEfDemo.DddEfDemo.Application.Shared;

public static class IdParsingExtensions
{
	/// <summary>
	/// <para>
	/// Extension method that attempts to parse the given <paramref name="idString"/> as a decimal, returning the result on success or 0 otherwise.
	/// </para>
	/// <para>
	/// The premise is that ID 0 can never be found, so any further validation can be handled by checking whether the expected object exists.
	/// </para>
	/// </summary>
	public static decimal ParseIdOrDefault(this string idString)
	{
		return Decimal.TryParse(idString, out var result)
			? result
			: default;
	}
}
