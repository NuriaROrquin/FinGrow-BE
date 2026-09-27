namespace FinGrow.Application.Jobs;

using System.Collections.Concurrent;

internal sealed class RunningJobs
{
    private readonly ConcurrentDictionary<string, byte> _names = new(StringComparer.Ordinal);

    public bool TryStart(string jobName) => _names.TryAdd(jobName, 0);

    public void Finish(string jobName) => _names.TryRemove(jobName, out _);
}
