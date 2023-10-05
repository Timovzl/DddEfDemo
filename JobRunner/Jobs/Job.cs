using Hangfire;

namespace Architect.DddEfDemo.DddEfDemo.JobRunner.Jobs;

/// <summary>
/// Do not inject this type directly. Instead, inject <see cref="IJob"/>.
/// </summary>
internal abstract class Job : IJob
{
	public abstract string CronSchedule { get; }

	[DisableConcurrentExecution(timeoutInSeconds: 5 * 60)]
	public abstract Task Execute(CancellationToken cancellationToken);
}
