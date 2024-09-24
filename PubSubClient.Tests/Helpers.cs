namespace PubSubClient.Tests;

internal static class Helpers
{
    internal static async Task<int> WaitUntil(Func<bool> condition, int frequency = 25, int timeout = -1)
    {
        int totalTime = 0;

        var waitTask = Task.Run(async () =>
        {
            while (!condition())
            {
                await Task.Delay(frequency);
                totalTime =+ 25;
            }
        });

        if (waitTask != await Task.WhenAny(waitTask, Task.Delay(timeout)))
            throw new TimeoutException();

        return totalTime;
    }
}
