using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Architect.DddEfDemo.DddEfDemo.Application;
using Architect.DddEfDemo.DddEfDemo.Infrastructure.Databases.Interceptors;

namespace Architect.DddEfDemo.DddEfDemo.Infrastructure.Databases;

public static class DatabaseInfrastructureRegistrationExtensions
{
	public static IServiceCollection AddDatabaseInfrastructureLayer(this IServiceCollection services, IConfiguration config)
	{
		var orphanedDomainObjectInterceptor = new OrphanedDomainObjectInterceptor();

		services.AddPooledDbContextFactory<CoreDbContext>(dbContext => dbContext
			.UseSqlServer(config.GetConnectionString("CoreDatabase")!, sqlServer => sqlServer.EnableRetryOnFailure())
			.AddInterceptors(new[]
			{
				orphanedDomainObjectInterceptor,
			}));

		services.AddDbContextScope<ICoreDatabase, CoreDbContext>(scope => scope
			.ExecutionStrategyOptions(ExecutionStrategyOptions.RetryOnOptimisticConcurrencyFailure)
			.AvoidFailureOnCommitRetries(true));

		services.AddMemoryCache();

		// Register the current project's dependencies
		services.Scan(scanner => scanner.FromAssemblies(typeof(DatabaseInfrastructureRegistrationExtensions).Assembly)
			.AddClasses(c => c.Where(type => !type.Name.Contains('<') && !type.IsNested && !type.Name.EndsWith("Interceptor")), publicOnly: false)
			.AsSelfWithInterfaces().WithSingletonLifetime());

		// For interceptors, always use the preconstructed instances already passed to EF
		services.AddSingleton(orphanedDomainObjectInterceptor);

		return services;
	}

	/// <summary>
	/// Causes all relevant database migrations to be applied on host startup, in a concurrency-safe manner.
	/// </summary>
	public static IServiceCollection AddDatabaseMigrations(this IServiceCollection services)
	{
		services.AddSingleton<MigrationAssistant<CoreDbContext>>();
		services.AddSingleton<IHostedService>(serviceProvider => serviceProvider.GetRequiredService<MigrationAssistant<CoreDbContext>>());

		return services;
	}
}
