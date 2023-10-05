using Hangfire;

namespace Architect.DddEfDemo.DddEfDemo.JobRunner.Jobs;

internal interface IJob
{
	string CronSchedule { get; }

	[DisableConcurrentExecution(timeoutInSeconds: 5 * 60)]
	Task Execute(CancellationToken cancellationToken);
}
