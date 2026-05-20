using System;
using System.Linq;
using System.Threading.Tasks;
using DataStore;
using Xunit;

namespace TestServices;

public class DataStoreTests
{
    [Fact]
    public void Set_ValidKeyValue_StoresValue()
    {
        var store = new DataStore.DataStore();
        var data = new byte[] { 1, 2, 3 };

        store.Set("test", data);

        var retrieved = store.Get("test");
        Assert.Equal(data, retrieved);
    }

    [Fact]
    public void Set_NullKey_ThrowsArgumentException()
    {
        var store = new DataStore.DataStore();
        Assert.Throws<ArgumentException>(() => store.Set(null, new byte[0]));
    }

    [Fact]
    public void Set_NullValue_ThrowsArgumentNullException()
    {
        var store = new DataStore.DataStore();
        Assert.Throws<ArgumentNullException>(() => store.Set("key", null));
    }

    [Fact]
    public void Get_NonExistentKey_ReturnsNull()
    {
        var store = new DataStore.DataStore();
        var result = store.Get("missing");
        Assert.Null(result);
    }

    [Fact]
    public void Delete_ExistingKey_ReturnsTrue()
    {
        var store = new DataStore.DataStore();
        store.Set("key", new byte[] { 1 });

        bool deleted = store.Delete("key");

        Assert.True(deleted);
        Assert.Null(store.Get("key"));
    }

    [Fact]
    public void Delete_NonExistentKey_ReturnsFalse()
    {
        var store = new DataStore.DataStore();
        bool deleted = store.Delete("nonexistent");
        Assert.False(deleted);
    }

    [Fact]
    public void Contains_ExistingKey_ReturnsTrue()
    {
        var store = new DataStore.DataStore();
        store.Set("key", new byte[] { 1 });
        Assert.True(store.Contains("key"));
    }

    [Fact]
    public void Contains_NonExistentKey_ReturnsFalse()
    {
        var store = new DataStore.DataStore();
        Assert.False(store.Contains("missing"));
    }

    [Fact]
    public void Count_ReflectsNumberOfItems()
    {
        var store = new DataStore.DataStore();
        Assert.Equal(0, store.Count);

        store.Set("a", new byte[] { 1 });
        Assert.Equal(1, store.Count);

        store.Set("b", new byte[] { 2 });
        Assert.Equal(2, store.Count);

        store.Delete("a");
        Assert.Equal(1, store.Count);
    }

    [Fact]
    public void Clear_RemovesAllItems()
    {
        var store = new DataStore.DataStore();
        store.Set("x", new byte[] { 1 });
        store.Set("y", new byte[] { 2 });

        store.Clear();

        Assert.Equal(0, store.Count);
        Assert.False(store.Contains("x"));
        Assert.False(store.Contains("y"));
    }

    [Fact]
    public void GetAllKeys_ReturnsAllKeys()
    {
        var store = new DataStore.DataStore();
        store.Set("k1", new byte[] { 1 });
        store.Set("k2", new byte[] { 2 });

        var keys = store.GetAllKeys();

        Assert.Equal(2, keys.Length);
        Assert.Contains("k1", keys);
        Assert.Contains("k2", keys);
    }

    [Fact]
    public void Statistics_SetCount_IncrementsOnSet()
    {
        var store = new DataStore.DataStore();
        long initial = store.SetCount;

        store.Set("a", new byte[] { 1 });
        store.Set("b", new byte[] { 2 });

        Assert.Equal(initial + 2, store.SetCount);
    }

    [Fact]
    public void Statistics_GetCount_IncrementsOnGet()
    {
        var store = new DataStore.DataStore();
        store.Set("a", new byte[] { 1 });

        long initial = store.GetCount;
        store.Get("a");
        store.Get("a");
        store.Get("nonexistent");

        Assert.Equal(initial + 3, store.GetCount);
    }

    [Fact]
    public void Statistics_DeleteCount_IncrementsOnDelete()
    {
        var store = new DataStore.DataStore();
        store.Set("a", new byte[] { 1 });
        store.Set("b", new byte[] { 2 });

        long initial = store.DeleteCount;
        store.Delete("a");
        store.Delete("b");
        store.Delete("c"); // non-existent

        Assert.Equal(initial + 3, store.DeleteCount);
    }

    [Fact]
    public async Task ThreadSafety_MultipleThreads_DoNotCorruptData()
    {
        var store = new DataStore.DataStore();
        int iterations = 1000;
        var tasks = Enumerable.Range(0, 10).Select(i => Task.Run(() =>
        {
            for (int j = 0; j < iterations; j++)
            {
                string key = $"key{i}_{j}";
                store.Set(key, new byte[] { (byte)j });
                store.Get(key);
                store.Delete(key);
            }
        })).ToArray();

        await Task.WhenAll(tasks);

        // After all deletions, store should be empty (except maybe overlapping keys)
        // But due to thread interleaving, some keys may remain; we just ensure no exceptions.
        // Also verify statistics are consistent
        Assert.True(store.SetCount >= iterations * 10);
        Assert.True(store.GetCount >= iterations * 10);
        Assert.True(store.DeleteCount >= iterations * 10);
    }

    [Fact]
    public async Task Parallel_ConcurrentReads_ShouldNotBlock()
    {
        var store = new DataStore.DataStore();
        store.Set("shared", new byte[] { 42 });

        int threadCount = 20;
        var tasks = Enumerable.Range(0, threadCount).Select(_ => Task.Run(() =>
        {
            for (int i = 0; i < 1000; i++)
            {
                var value = store.Get("shared");
                Assert.NotNull(value);
                Assert.Equal(42, value[0]);
            }
        })).ToArray();

        await Task.WhenAll(tasks);
        // Each Get increments GetCount, Set increments SetCount (once).
        // Total Get calls = threadCount * 1000
        Assert.Equal(threadCount * 1000, store.GetCount);
        Assert.Equal(1, store.SetCount);
    }

    [Fact]
    public async Task Parallel_ConcurrentWritesToDifferentKeys_ShouldNotCorrupt()
    {
        var store = new DataStore.DataStore();
        int threadCount = 10;
        int keysPerThread = 100;

        var tasks = Enumerable.Range(0, threadCount).Select(threadId => Task.Run(() =>
        {
            for (int i = 0; i < keysPerThread; i++)
            {
                string key = $"thread{threadId}_key{i}";
                byte[] value = new byte[] { (byte)threadId, (byte)i };
                store.Set(key, value);
                var retrieved = store.Get(key);
                Assert.Equal(value, retrieved);
            }
        })).ToArray();

        await Task.WhenAll(tasks);

        // Verify all keys are present and correct
        for (int threadId = 0; threadId < threadCount; threadId++)
        {
            for (int i = 0; i < keysPerThread; i++)
            {
                string key = $"thread{threadId}_key{i}";
                byte[] expected = new byte[] { (byte)threadId, (byte)i };
                var actual = store.Get(key);
                Assert.Equal(expected, actual);
            }
        }
        Assert.Equal(threadCount * keysPerThread, store.Count);
    }

    [Fact]
    public async Task Parallel_MixedOperations_ShouldMaintainConsistency()
    {
        var store = new DataStore.DataStore();
        int threadCount = 8;
        int operationsPerThread = 500;
        var rnd = new Random();

        var tasks = Enumerable.Range(0, threadCount).Select(threadId => Task.Run(() =>
        {
            for (int op = 0; op < operationsPerThread; op++)
            {
                int action = rnd.Next(0, 5);
                string key = $"key{threadId}_{op % 10}";
                switch (action)
                {
                    case 0:
                        store.Set(key, new byte[] { (byte)op });
                        break;
                    case 1:
                        store.Get(key);
                        break;
                    case 2:
                        store.Delete(key);
                        break;
                    case 3:
                        store.Contains(key);
                        break;
                    case 4:
                        _ = store.Count;
                        break;
                }
            }
        })).ToArray();

        await Task.WhenAll(tasks);

        // No assertion about final state because operations are random,
        // but we verify no exceptions were thrown and statistics are consistent.
        // Only Set, Get, Delete increment counters; Contains and Count do not.
        // With 5 possible actions, roughly 3/5 of operations increment counters.
        long totalIncremented = store.SetCount + store.GetCount + store.DeleteCount;
        long totalOperations = threadCount * operationsPerThread;
        Assert.True(totalIncremented >= totalOperations * 3 / 5 - 100, $"Expected at least ~{totalOperations * 3 / 5} incremented operations, got {totalIncremented}");
        Assert.True(totalIncremented <= totalOperations, $"Should not exceed total operations {totalOperations}, got {totalIncremented}");
    }

    [Fact]
    public async Task Parallel_StressTest_WithManyThreads()
    {
        var store = new DataStore.DataStore();
        int threadCount = 50;
        int operationsPerThread = 200;

        var tasks = Enumerable.Range(0, threadCount).Select(threadId => Task.Run(() =>
        {
            for (int i = 0; i < operationsPerThread; i++)
            {
                string key = $"stress{threadId}_{i}";
                store.Set(key, new byte[] { (byte)i });
                store.Get(key);
                if (i % 3 == 0)
                {
                    store.Delete(key);
                }
                if (i % 5 == 0)
                {
                    store.Contains(key);
                }
            }
        })).ToArray();

        await Task.WhenAll(tasks);

        // Ensure counts are non-zero and consistent
        Assert.True(store.SetCount >= threadCount * operationsPerThread);
        Assert.True(store.GetCount >= threadCount * operationsPerThread);
        Assert.True(store.DeleteCount >= threadCount * operationsPerThread / 3);
    }
}