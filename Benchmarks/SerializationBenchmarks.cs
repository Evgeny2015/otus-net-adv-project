using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using DataStore;
using System;
using System.IO;
using System.Text.Json;

namespace Benchmarks;

[MemoryDiagnoser]
public class SerializationBenchmarks
{
    private MovingObject _movingObject;

    [GlobalSetup]
    public void Setup()
    {
        // Create a sample MovingObject with realistic data
        _movingObject = new MovingObject(37.6176, 55.7558) // Moscow coordinates
        {
            Altitude = 156.0,
            Accuracy = 5.2,
            VerticalAccuracy = 3.1,
            Speed = 12.5,
            Direction = 45.0,
            MagneticVariation = 7.2,
            Acceleration = new Acceleration
            {
                Linear = 0.5,
                Lateral = 0.2,
                Vertical = 0.1
            },
            LastUpdate = DateTime.UtcNow
        };
    }

    [Benchmark(Baseline = true)]
    public byte[] JsonSerialize()
    {
        var json = JsonSerializer.Serialize(_movingObject);
        return System.Text.Encoding.UTF8.GetBytes(json);
    }

    [Benchmark]
    public byte[] BinarySerializeGenerated()
    {
        using var memoryStream = new MemoryStream();
        _movingObject.SerializeToBinary(memoryStream);
        return memoryStream.ToArray();
    }
}

public class Program
{
    public static void Main(string[] args)
    {
        var summary = BenchmarkRunner.Run<SerializationBenchmarks>();
    }
}