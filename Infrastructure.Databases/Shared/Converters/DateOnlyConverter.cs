using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Architect.DddEfDemo.DddEfDemo.Infrastructure.Databases.Shared.Converters;

/// <summary>
/// Converts <see cref="DateOnly"/> values so that the database provider understands them.
/// </summary>
internal sealed class DateOnlyConverter : ValueConverter<DateOnly, DateTime>
{
	public DateOnlyConverter()
		: base(codeValue => codeValue.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc), dbValue => DateOnly.FromDateTime(dbValue))
	{
	}
}
