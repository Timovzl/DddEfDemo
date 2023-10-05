using Microsoft.EntityFrameworkCore.Design;

namespace Architect.DddEfDemo.DddEfDemo.Infrastructure.Databases.Shared;

/// <summary>
/// Used by the manual migration tool to construct the <see cref="DbContext"/>.
/// </summary>
internal sealed class CoreDbContextFactory : IDesignTimeDbContextFactory<CoreDbContext>
{
	public CoreDbContext CreateDbContext(string[] args)
	{
		// Pooling should be disabled in design-time migrations, to avoid connection issues caused by ALTER DATABASE queries
		var optionsBuilder = new DbContextOptionsBuilder<CoreDbContext>();
		optionsBuilder.UseSqlServer(@"Data Source=(LocalDB)\MSSQLLocalDB;Integrated Security=True;Initial Catalog=__ToDoAreaName__.__ToDoBoundedContextName__;Connect Timeout=5;Pooling=False;");

		return new CoreDbContext(optionsBuilder.Options);
	}
}
