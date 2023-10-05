using Hangfire.Common;
using Hangfire.Server;
using System.Runtime.CompilerServices;

namespace Architect.DddEfDemo.DddEfDemo.JobRunner;

/// <summary>
/// Adds job metadata to the ambient <see cref="ILogger"/> scope for the duration of <see cref="Hangfire"/> job runs.
/// </summary>
internal sealed class HangfireJobLogEnricherAttribute : JobFilterAttribute, IServerFilter
{
	private static readonly ConditionalWeakTable<string, IDisposable> LogScopes = new ConditionalWeakTable<string, IDisposable>();

	private ILogger<HangfireJobLogEnricherAttribute> Logger { get; }

	public HangfireJobLogEnricherAttribute(ILogger<HangfireJobLogEnricherAttribute> logger)
	{
		this.Logger = logger;
	}

	public void OnPerforming(PerformingContext filterContext)
	{
		IDisposable? logScope = null;
		try
		{
			logScope = this.Logger.BeginScope(new KeyValuePair<string, object>[]
			{
				KeyValuePair.Create("Job", (object)filterContext.BackgroundJob.Job.Type.Name),
				KeyValuePair.Create("JobRunId", (object)filterContext.BackgroundJob.Id),
			});

			if (logScope is not null)
				LogScopes.Add(filterContext.BackgroundJob.Id, logScope);
		}
		catch
		{
			logScope?.Dispose();
			throw;
		}
	}

	public void OnPerformed(PerformedContext filterContext)
	{
		if (LogScopes.TryGetValue(filterContext.BackgroundJob.Id, out var logScope))
		{
			logScope.Dispose();
			LogScopes.Remove(filterContext.BackgroundJob.Id);
		}
	}
}
