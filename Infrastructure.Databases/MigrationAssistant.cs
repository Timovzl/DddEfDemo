using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Architect.DddEfDemo.DddEfDemo.Infrastructure.Databases;

/// <summary>
/// <para>
/// Helps perform migrations without concurrency conflicts.
/// </para>
/// <para>
/// Although <see cref="MigrateAsync"/> can be invoked directly, that method is normally invoked by registering this type as <see cref="IHostedService"/> and starting the host.
/// </para>
/// </summary>
internal sealed class MigrationAssistant<TDbContext> : IHostedService
	where TDbContext : DbContext
{
	private ILogger<MigrationAssistant<TDbContext>> Logger { get; }
	private IDbContextFactory<TDbContext> DbContextFactory { get; }

	public MigrationAssistant(
		ILogger<MigrationAssistant<TDbContext>> logger,
		IDbContextFactory<TDbContext> dbContextFactory)
	{
		this.Logger = logger;
		this.DbContextFactory = dbContextFactory;
	}

	public Task StartAsync(CancellationToken cancellationToken)
	{
		return this.MigrateAsync(cancellationToken);
	}

	public Task StopAsync(CancellationToken cancellationToken)
	{
		return Task.CompletedTask; // Nothing to do
	}

	/// <summary>
	/// Performs database migrations for <typeparamref name="TDbContext"/> in a concurrency-safe way.
	/// </summary>
	public async Task MigrateAsync(CancellationToken cancellationToken)
	{
		// Ensure that the database exists
		// The DbContext must be disposed immediately after this, since a potential ALTER DATABASE query may close the connection from the server's end
		await using (var dbContext = await this.DbContextFactory.CreateDbContextAsync(cancellationToken))
		{
			var appliedMigrations = await dbContext.Database.GetAppliedMigrationsAsync(cancellationToken);
			if (!appliedMigrations.Any())
			{
				this.Logger.LogInformation("Creating {DbContext} database by migrating to version 0", typeof(TDbContext).Name);
				await this.PerformMigrationZeroAsync(cancellationToken);
			}
		}

		this.Logger.LogInformation("Awaiting exclusive lock to migrate {DbContext}", typeof(TDbContext).Name);

		await using var migrationLock = await MigrationLock.AcquireAsync(this.DbContextFactory, cancellationToken);

		// Overwrite the cancellation token so that it also honors the migration lock's token
		using var combinedCancellationSource = CancellationTokenSource.CreateLinkedTokenSource(migrationLock.CancellationToken, cancellationToken);
		cancellationToken = combinedCancellationSource.Token;

		this.Logger.LogInformation("Migrating {DbContext}", typeof(TDbContext).Name);

		await using (var dbContext = await this.DbContextFactory.CreateDbContextAsync(cancellationToken))
			await dbContext.Database.MigrateAsync(cancellationToken);

		this.Logger.LogInformation("Migrated {DbContext}", typeof(TDbContext).Name);
	}

	/// <summary>
	/// <para>
	/// Attempts to have Entity Framework create the database, by migrating to version 0.
	/// </para>
	/// <para>
	/// If the very first migration that exists has "Collation" in its name, this method migrates to that instead.
	/// This can be used to set the database collation before we acquire a lock that would prevent database alterations.
	/// </para>
	/// <para>
	/// Unfortunately, with no database to use for locking, we make this method concurrency-safe: it could migrate <em>back</em> to version 0.
	/// Luckily, it only comes into play when the database is initially being created.
	/// </para>
	/// </summary>
	private async Task PerformMigrationZeroAsync(CancellationToken cancellationToken)
	{
		await using var dbContext = await this.DbContextFactory.CreateDbContextAsync(cancellationToken);

		// If there is an initial migration that sets the database collation, migrate to it (in case the database already existed but had the wrong collation, e.g. in non-local environments)
		// Otherwise, migrate to "0", i.e. just before the first migration
		var pendingMigrations = await dbContext.Database.GetPendingMigrationsAsync(cancellationToken);
		var firstMigration = pendingMigrations.FirstOrDefault();
		firstMigration = firstMigration?.Contains("Collation", StringComparison.OrdinalIgnoreCase) == true
			? firstMigration
			: "0";

		var migrator = dbContext.GetInfrastructure().GetRequiredService<IMigrator>();

		try
		{
			await migrator.MigrateAsync(firstMigration, cancellationToken);
		}
		catch (SqlException e) when (e.Number == 1801) // SQL Server: Database already exists
		{
			// We lost a race condition creating the database, so we will wait a moment to allow the winner to perform migration zero
			await Task.Delay(TimeSpan.FromSeconds(2), cancellationToken);
		}
		finally
		{
			// Since an ALTER DATABASE breaks the connection from the server's end, clear our connection pool
			await dbContext.Database.CloseConnectionAsync();
			SqlConnection.ClearPool((SqlConnection)dbContext.Database.GetDbConnection());
		}
	}

	/// <summary>
	/// <para>
	/// Represents an exclusive lock for the application to perform migrations using the <see cref="MigrationAssistant{TDbContext}"/>.
	/// </para>
	/// <para>
	/// To avoid concurrency conflicts, any migration work <em>must</em> honor <see cref="CancellationToken"/>.
	/// </para>
	/// </summary>
	private sealed class MigrationLock : IAsyncDisposable
	{
		private DbContext DbContext { get; }
		/// <summary>
		/// Used to relinquish our lock when we are disposed, such as when the calling code has completed its work.
		/// </summary>
		private CancellationTokenSource DisposalSource { get; } = new CancellationTokenSource();
		/// <summary>
		/// Used to cancel the token exposed to the calling code if we lose or relinquish the exclusive lock.
		/// </summary>
		private CancellationTokenSource LockInterruptionSource { get; } = new CancellationTokenSource();

		private Task SleepTask { get; set; } = Task.CompletedTask;

		/// <summary>
		/// The calling code must honor this token. It is cancelled when the exclusive lock is lost or relinquished.
		/// </summary>
		public CancellationToken CancellationToken => this.LockInterruptionSource.Token;

		/// <summary>
		/// <para>
		/// Returns an exclusive lock for the application to perform migrations using the <see cref="MigrationAssistant"/>.
		/// </para>
		/// <para>
		/// To avoid concurrency conflicts, any migration work <em>must</em> honor the returned object's <see cref="CancellationToken"/>.
		/// </para>
		/// <para>
		/// The returned object <em>must</em> be disposed, asynchronously.
		/// </para>
		/// </summary>
		public static async Task<MigrationLock> AcquireAsync(IDbContextFactory<TDbContext> dbContextFactory, CancellationToken cancellationToken)
		{
			var result = new MigrationLock(dbContextFactory);

			try
			{
				await result.InitializeAsync(cancellationToken);
				return result;
			}
			catch
			{
				await result.DisposeAsync();
				throw;
			}
		}

		private MigrationLock(IDbContextFactory<TDbContext> dbContextFactory)
		{
			this.DbContext = dbContextFactory.CreateDbContext();
		}

		private async Task InitializeAsync(CancellationToken cancellationToken)
		{
			// If we have to wait for the lock, have the same patience as the winning replica
			this.DbContext.Database.SetCommandTimeout(TimeSpan.FromHours(12));

			// Ensure that our locking table exists
			await this.DbContext.Database.ExecuteSqlRawAsync(@"
IF OBJECT_ID(N'dbo.__EFMigrationsLock', N'U') IS NULL BEGIN

CREATE TABLE dbo.__EFMigrationsLock (
	Id tinyint not null PRIMARY KEY,
	CreationDateTime datetime2 not null default CURRENT_TIMESTAMP
);

END;
", cancellationToken);

			await this.DbContext.Database.BeginTransactionAsync(cancellationToken);

			// Obtain an exclusive lock
			await this.DbContext.Database.ExecuteSqlRawAsync(@"INSERT INTO dbo.__EFMigrationsLock (Id) VALUES (1);", cancellationToken);

			// Have our database connection sleep as a timeout, or until the calling code succeeds
			this.SleepTask = this.DbContext.Database.ExecuteSqlRawAsync(@"WAITFOR DELAY '12:00:00';", this.DisposalSource.Token);

			// If the database transaction gets disrupted for any reason, follow up by cancelling our lock interruption source
			this.SleepTask = this.SleepTask.ContinueWith(_ => this.LockInterruptionSource.Cancel(), CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
		}

		public async ValueTask DisposeAsync()
		{
			try
			{
				// Stop the database sleep operation, if any
				this.DisposalSource.Cancel();

				// Wait for the sleep task to be interrupted
				// The ongoing query needs to be stopped before we can perform a rollback query
				await this.SleepTask;

				// Attempt to roll back any ongoing transaction
				if (this.DbContext.Database.CurrentTransaction is not null)
					await this.DbContext.Database.RollbackTransactionAsync();
			}
			finally
			{
				await this.DbContext.DisposeAsync();
			}
		}
	}
}
