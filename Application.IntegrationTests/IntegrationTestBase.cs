using System.Data;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Architect.EntityFramework.DbContextManagement;
using Architect.DddEfDemo.DddEfDemo.Application.Shared;
using Architect.DddEfDemo.DddEfDemo.Infrastructure.Databases;
using Architect.DddEfDemo.DddEfDemo.Infrastructure.Databases.Interceptors;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Moq;

namespace Architect.DddEfDemo.DddEfDemo.Application.IntegrationTests;

public abstract class IntegrationTestBase : IDisposable
{
	/// <summary>
	/// The current time zone's offset from UTC during January. Useful for replacements in JSON strings to make assertions on.
	/// </summary>
	protected static string TimeZoneUtcOffsetString { get; } = $"+{TimeZoneInfo.Local.GetUtcOffset(DateTime.UnixEpoch):hh\\:mm}";
	protected static JsonSerializerOptions JsonSerializerOptions { get; } = new JsonSerializerOptions() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, Converters = { new JsonStringEnumConverter() } };
	
	/// <summary>
	/// A fixed timestamp on January 1 in the future, matching <see cref="FixedTime"/>, but without sub-millisecond components.
	/// The nonzero time components help test edge cases, such as rounding or truncation by the database.
	/// </summary>
	protected static readonly DateTime RoundedFixedTime = new DateTime(3000, 01, 01, 01, 01, 01, millisecond: 01, DateTimeKind.Utc);
	/// <summary>
	/// A fixed timestamp on January 1 in the future, with a nonzero value for hours, minutes, seconds, milliseconds, and ticks.
	/// The nonzero time components help test edge cases, such as rounding or truncation by the database.
	/// </summary>
	protected static readonly DateTime FixedTime = new DateTime(3000, 01, 01, 01, 01, 01, millisecond: 01, DateTimeKind.Utc).AddTicks(1);
	/// <summary>
	/// A fixed date on January 1 in the future, matching the date of <see cref="FixedTime"/>.
	/// </summary>
	protected static readonly DateOnly FixedDate = DateOnly.FromDateTime(FixedTime);

	protected string UniqueTestName { get; } = $"Test_{Guid.NewGuid():N}";

	protected IHostBuilder HostBuilder { get; set; }

	protected IConfiguration Configuration { get; }
	protected string ConnectionString { get; }

	/// <summary>
	/// <para>
	/// Returns the host, which contains the services.
	/// </para>
	/// <para>
	/// On the first resolution, the host is built and started.
	/// </para>
	/// <para>
	/// If the host is started, it is automatically stopped when the test class is disposed.
	/// </para>
	/// </summary>
	protected IHost Host
	{
		get
		{
			if (this._host is null)
			{
				this._host = this.HostBuilder.Build();
				this.CreateDatabase();
				this._host.Start();
			}
			return this._host;
		}
	}
	private IHost? _host;

	protected IntegrationTestBase()
	{
		this.HostBuilder = Microsoft.Extensions.Hosting.Host.CreateDefaultBuilder()
			.UseDefaultServiceProvider(provider => provider.ValidateOnBuild = provider.ValidateScopes = true) // Be as strict as ASP.NET Core in Development is
			.ConfigureWebHostDefaults(webBuilder => webBuilder
				.Configure(appBuilder => appBuilder
					.UseRouting()
					.UseApplicationControllers())
				.UseTestServer(options => options.PreserveExecutionContext = true));

		this.Configuration = new ConfigurationBuilder()
			.AddJsonFile("appsettings.json")
			.AddJsonFile($"appsettings.{Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")}.json", optional: true)
			.AddEnvironmentVariables()
			.Build();

		this.ConnectionString = $@"{this.Configuration["ConnectionStrings:CoreDatabase"]};Initial Catalog={this.UniqueTestName};";
		this.Configuration["ConnectionStrings:CoreDatabase"] = this.ConnectionString;

		this.ConfigureServices(services => services.AddSingleton(this.Configuration));

		HashSet<ServiceDescriptor> existingHostedServices = null!;
		this.ConfigureServices(services => existingHostedServices = services.Where(descriptor => descriptor.ServiceType == typeof(IHostedService)).ToHashSet());

		this.ConfigureServices(services => services.AddApplicationLayer(this.Configuration));
		this.ConfigureServices(services => services.AddDatabaseInfrastructureLayer(this.Configuration));

		this.ConfigureServices(services => services.AddSingleton(new Mock<IJobEnqueuer>().Object));

		// Remove custom hosted services, to avoid running startup/background logic
		this.ConfigureServices(services =>
		{
			foreach (var descriptor in services.Where(descriptor => descriptor.ServiceType == typeof(IHostedService) && !existingHostedServices.Contains(descriptor)).ToList())
				services.Remove(descriptor);
		});

		this.ConfigureServices(services => services
			.AddApplicationControllers()
			.AddApplicationPart(typeof(Api.Program).Assembly));

		// We normally throw when events are orphaned when saving changes
		// We want to avoid doing so for events produced during test arrangements
		// As such, we disable the responsible interceptor until the very last moment, i.e. when any use case is resolved from the container
		this.ConfigureServices(services =>
		{
			var interceptorDescriptor = services.Single(descriptor => descriptor.ImplementationInstance is OrphanedDomainObjectInterceptor);
			((OrphanedDomainObjectInterceptor)interceptorDescriptor.ImplementationInstance!).Disable(); // Disable the interceptor initially

			foreach (var descriptor in services.Where(descriptor => descriptor.ServiceType.Name.EndsWith("UseCase")).ToList())
			{
				services.Remove(descriptor);
				services.Add(new ServiceDescriptor(descriptor.ServiceType, serviceProvider =>
				{
					((OrphanedDomainObjectInterceptor)interceptorDescriptor.ImplementationInstance!).Enable(); // Enable the interceptor now that the use case is being resolved
					return ActivatorUtilities.CreateInstance(serviceProvider, descriptor.ImplementationType!);
				}, descriptor.Lifetime));
			}
		});
	}

	public virtual void Dispose()
	{
		GC.SuppressFinalize(this);

		try
		{
			this._host?.StopAsync().GetAwaiter().GetResult();
		}
		finally
		{
			this._host?.Dispose();

			if (this._host is not null)
				this.DeleteDatabase();
		}
	}

	/// <summary>
	/// Adds an action to be executed as part of what would normally be Startup.ConfigureServices().
	/// </summary>
	protected void ConfigureServices(Action<IServiceCollection> action)
	{
		if (this._host is not null)
			throw new NotSupportedException("No more services can be registered once the host is resolved.");

		this.HostBuilder.ConfigureServices(action ?? throw new ArgumentNullException(nameof(action)));
	}

	/// <summary>
	/// <para>
	/// Performs a request as if it came from the outside, using the middleware pipeline, for maximum coverage.
	/// </para>
	/// <para>
	/// Throws an <see cref="HttpRequestException"/> if the response has a non-success status code.
	/// </para>
	/// <para>
	/// The request is handled in the current ExecutionContext, with access to the test's ambient contexts, such as ClockScope.
	/// </para>
	/// </summary>
	protected async Task GetApiResponse<TRequest>(HttpMethod verb, string route, TRequest request)
	{
		using var httpClient = this.Host.GetTestClient();

		var requestJson = JsonSerializer.Serialize(request);

		using var requestMessage = new HttpRequestMessage(verb, route)
		{
			Content = new StringContent(requestJson, Encoding.UTF8, "application/json"),
		};

		using var response = await httpClient.SendAsync(requestMessage);

		if (!response.IsSuccessStatusCode)
		{
			var errorMessage = await response.Content.ReadAsStringAsync();
			if (String.IsNullOrEmpty(errorMessage)) errorMessage = response.StatusCode.ToString();
			throw new HttpRequestException(errorMessage);
		}
	}

	/// <summary>
	/// <para>
	/// Performs a request as if it came from the outside, using the middleware pipeline, for maximum coverage.
	/// </para>
	/// <para>
	/// Throws an <see cref="HttpRequestException"/> if the response has a non-success status code.
	/// </para>
	/// <para>
	/// The request is handled in the current ExecutionContext, with access to the test's ambient contexts, such as ClockScope.
	/// </para>
	/// </summary>
	protected async Task<TResponse?> GetApiResponse<TRequest, TResponse>(HttpMethod verb, string route, TRequest request)
	{
		using var httpClient = this.Host.GetTestClient();

		using var requestMessage = new HttpRequestMessage(verb, route);

		if (verb == HttpMethod.Get)
		{
			if (request is not null)
			{
				var culture = new CultureInfo(CultureInfo.InvariantCulture.Name);
				culture.DateTimeFormat.ShortDatePattern = "yyyy-MM-ddTHH:mm:ss.fffK";
				culture.DateTimeFormat.LongTimePattern = "";

				var queryParameters = typeof(TRequest).GetProperties()
					.Select(property => $"{property.Name}={Uri.EscapeDataString(Convert.ToString(property.GetValue(request), culture) ?? "")}");

				requestMessage.RequestUri = new Uri($"{requestMessage.RequestUri}?{String.Join('&', queryParameters)}", UriKind.Relative);
			}
		}
		else
		{
			var requestJson = JsonSerializer.Serialize(request, JsonSerializerOptions);
			requestMessage.Content = new StringContent(requestJson, Encoding.UTF8, "application/json");
		}

		using var response = await httpClient.SendAsync(requestMessage);

		if (!response.IsSuccessStatusCode)
		{
			var errorMessage = await response.Content.ReadAsStringAsync();
			if (String.IsNullOrEmpty(errorMessage)) errorMessage = response.StatusCode.ToString();
			throw new HttpRequestException(errorMessage);
		}

		var responseJson = await response.Content.ReadAsStringAsync();
		var result = JsonSerializer.Deserialize<TResponse>(responseJson, JsonSerializerOptions);

		return result;
	}

	private protected DbContextScope<CoreDbContext> CreateDbContextScope()
	{
		return (DbContextScope<CoreDbContext>)this.Host.Services.GetRequiredService<IDbContextProvider<CoreDbContext>>().CreateDbContextScope();
	}

	protected async Task<int> ExecuteNonQuery(string query)
	{
		var dbContextAccessor = this.Host.Services.GetRequiredService<IDbContextAccessor<CoreDbContext>>();

		await using var temporaryDbContext = dbContextAccessor.HasDbContext
			? null
			: await this.Host.Services.GetRequiredService<IDbContextFactory<CoreDbContext>>().CreateDbContextAsync();

		var dbContext = dbContextAccessor.HasDbContext
			? dbContextAccessor.CurrentDbContext
			: temporaryDbContext!;

		return await dbContext.Database.ExecuteSqlRawAsync(query);
	}

	protected async Task<object?> ExecuteScalar(string query)
	{
		var dbContextAccessor = this.Host.Services.GetRequiredService<IDbContextAccessor<CoreDbContext>>();

		await using var temporaryDbContext = dbContextAccessor.HasDbContext
			? null
			: await this.Host.Services.GetRequiredService<IDbContextFactory<CoreDbContext>>().CreateDbContextAsync();

		var dbContext = dbContextAccessor.HasDbContext
			? dbContextAccessor.CurrentDbContext
			: temporaryDbContext!;

		var connection = dbContext.Database.GetDbConnection();
		var hadExistingConnection = connection.State == ConnectionState.Open;
		if (!hadExistingConnection)
			await dbContext.Database.OpenConnectionAsync();

		try
		{
			using var command = connection.CreateCommand();
			command.Transaction = dbContext.Database.CurrentTransaction?.GetDbTransaction();
			command.CommandText = query;

			return await command.ExecuteScalarAsync();
		}
		finally
		{
			if (!hadExistingConnection)
				await dbContext.Database.CloseConnectionAsync();
		}
	}

	/// <summary>
	/// Creates the database, ensuring that it exists so that Initial Catalog can then be used to isolate the entire test to its own database.
	/// </summary>
	private void CreateDatabase()
	{
		// To create or delete the database, we must connect without specifying it in the connection string
		var connectionString = Regex.Replace(this.ConnectionString, "Initial Catalog=[^;]+", "");

		using (var connection = new SqlConnection(connectionString))
		using (var command = connection.CreateCommand())
		{
			command.CommandText = $"CREATE DATABASE {this.UniqueTestName} COLLATE {CoreDbContext.DefaultCollation};";

			connection.Open();
			command.ExecuteNonQuery();
		}

		using var dbContextScope = this.Host.Services.GetRequiredService<IDbContextProvider<CoreDbContext>>().CreateDbContextScope();
		dbContextScope.DbContext.Database.EnsureCreated();
	}

	/// <summary>
	/// <para>
	/// Deletes the current test's database if it exists.
	/// </para>
	/// <para>
	/// Note that the <em>DbContext's</em> connection string must use Pooling=False to avoid leftover open connections in the pool blocking the drop operation.
	/// (Working alternative: https://stackoverflow.com/a/7469167/543814.)
	/// </para>
	/// </summary>
	private void DeleteDatabase()
	{
		// To create or delete the database, we must connect without specifying it in the connection string
		var connectionString = Regex.Replace(this.ConnectionString, "Initial Catalog=[^;]+", "");

		using var connection = new SqlConnection(connectionString);
		using var command = connection.CreateCommand();

		command.CommandText = $"DROP DATABASE IF EXISTS {this.UniqueTestName};";

		connection.Open();
		command.ExecuteNonQuery();
	}
}
