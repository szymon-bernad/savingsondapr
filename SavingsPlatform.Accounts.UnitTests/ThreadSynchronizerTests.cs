using SavingsPlatform.Common.Helpers;
using Xunit;

namespace SavingsPlatform.Accounts.UnitTests;

public class ThreadSynchronizerTests
{
    class TestRecord
    {
        public string Id { get; set; }

        public int Value { get; set; }
    }

    [Fact]
    public async Task Execute_ShouldRunSynchronously()
    {
        var sut = new ThreadSynchronizer();
        var rnd = new Random();

        var record1 = new TestRecord { Id = string.Empty, Value = 0 };
        int maxValue1 = 0;
        int minValue1 = 0;

        var record2 = new TestRecord { Id = string.Empty, Value = 0 };
        int maxValue2 = 0;
        int minValue2 = 0;

        var funcTask = () => Task.Run(async () =>
        {
            var id = Guid.NewGuid().ToString();

            record1.Id = id;
            record1.Value += 1;
            maxValue1 = Math.Max(maxValue1, record1.Value);

            await Task.Delay(rnd.Next(10));
            if (record1.Id == id)
            {
                record1.Value -= 1;
            }
            minValue1 = Math.Min(minValue1, record1.Value);
        });

        var funcTask2 = () => Task.Run(async () =>
        {
            var id = Guid.NewGuid().ToString();

            record2.Id = id;
            record2.Value += 10;
            maxValue2 = Math.Max(maxValue2, record2.Value);

            if (record2.Id == id)
            {
                record2.Value -= 10;
            }
            minValue2 = Math.Min(minValue2, record2.Value);
        });

        await Parallel.ForEachAsync(
            Enumerable.Range(0, 96),
            new ParallelOptions { MaxDegreeOfParallelism = 8 },
            async (_,_) => 
            {
                var type = rnd.Next(100) < 50 ? 1 : 2;
                await sut.ExecuteSynchronizedAsync($"test-key-{type}", type == 1 ? funcTask : funcTask2); 
            });

        Assert.Equal(0, record1.Value);
        Assert.Equal(0, minValue1);
        Assert.Equal(1, maxValue1);

        Assert.Equal(0, record2.Value);
        Assert.Equal(0, minValue2);
        Assert.Equal(10, maxValue2);
    }
}
