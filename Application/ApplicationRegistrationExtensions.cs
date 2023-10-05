using System.Globalization;
using System.Text.Json.Serialization;
using Architect.DddEfDemo.DddEfDemo.Domain;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Architect.DddEfDemo.DddEfDemo.Application;

public static class ApplicationRegistrationExtensions
{
	public static IServiceCollection AddApplicationLayer(this IServiceCollection services, IConfiguration configuration)
	{
		// Use the invariant culture throughout the application
		CultureInfo.DefaultThreadCurrentCulture = CultureInfo.CurrentCulture = CultureInfo.InvariantCulture;

		// Register the layers that we depend on
		services.AddDomainLayer(configuration);

		// Register the current project's dependencies
		services.Scan(scanner => scanner.FromAssemblies(typeof(ApplicationRegistrationExtensions).Assembly)
			.AddClasses(c => c.Where(type => type.Name.EndsWith("er") || type.Name.EndsWith("or") || type.Name.EndsWith("UseCase") || type.Name.EndsWith("Client")), publicOnly: false) // Services only
			.AsSelfWithInterfaces().WithSingletonLifetime());
		
		services.AddHttpContextAccessor();

		return services;
	}

	public static IMvcBuilder AddApplicationControllers(this IServiceCollection services)
	{
		// Consistently use our own exception handling, irrespective of whether ASP.NET Core considers the model valid
		services.Configure<ApiBehaviorOptions>(options => options.SuppressModelStateInvalidFilter = true);

		var result = services.AddControllers()
			.AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));

		// AddAuthentication() could be added here if authentication is required

		return result;
	}

	public static IApplicationBuilder UseApplicationControllers(this IApplicationBuilder applicationBuilder)
	{
		// UseAuthentication() and UseAuthorization() could be added here if authentication and/or authorization is required (both required even to just have authentication)

		applicationBuilder.UseEndpoints(endpoints => endpoints
			.MapControllers()); // RequireAuthorization() could be added here if authentication and/or authorization is required

		return applicationBuilder;
	}
}
