using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Architect.DddEfDemo.DddEfDemo.Infrastructure.Databases.Shared.Converters;

/// <summary>
/// Converts <see cref="DateTime"/> values so that values from the database are interpreted as <see cref="DateTimeKind.Utc"/>.
/// </summary>
internal sealed class UtcDateTimeConverter : ValueConverter<DateTime, DateTime>
{
	public UtcDateTimeConverter()
		: base(codeValue => codeValue, dbValue => DateTime.SpecifyKind(dbValue, DateTimeKind.Utc))
	{
	}
}
