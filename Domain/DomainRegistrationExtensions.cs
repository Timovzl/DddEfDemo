using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Architect.DddEfDemo.DddEfDemo.Domain;

public static class DomainRegistrationExtensions
{
	public static IServiceCollection AddDomainLayer(this IServiceCollection services, IConfiguration _)
	{
		// Register the current project's dependencies
		services.Scan(scanner => scanner.FromAssemblies(typeof(DomainRegistrationExtensions).Assembly)
			.AddClasses(c => c.Where(type => type.GetInterface(typeof(IDomainService).FullName!) is not null), publicOnly: false)
			.AsSelfWithInterfaces().WithSingletonLifetime());

		return services;
	}
}
