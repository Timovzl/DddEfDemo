namespace Architect.DddEfDemo.DddEfDemo.Testing.Common.Extensions;

public static class DateTimeExtensions
{
	/// <summary>
	/// Rounds the given <see cref="DateTime"/> in the same way the database would.
	/// </summary>
	public static DateTime Round(this DateTime dateTime)
	{
		var submillisecondTicks = dateTime.Ticks % 10_000;
		var result = dateTime.AddTicks(-submillisecondTicks);
		return result;
	}
}
