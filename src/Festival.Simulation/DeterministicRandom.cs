using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;

namespace Festival.Simulation;

// Generator v2: specified SplitMix64 stream, independent of runtime Random/hash behavior.
public sealed class DeterministicRandom(ulong seed)
{
    private ulong state = seed;

    internal static DeterministicRandom For(ScenarioDefinition definition, int day, string stream)
    {
        var key = FormattableString.Invariant(
            $"{definition.ScenarioId.Length}:{definition.ScenarioId}/{definition.GeneratorVersion}/{definition.Seed}/{day}/{stream}");
        return new DeterministicRandom(BinaryPrimitives.ReadUInt64BigEndian(
            SHA256.HashData(Encoding.UTF8.GetBytes(key))));
    }

    public long NextInt64(long exclusiveMaximum)
    {
        if (exclusiveMaximum <= 0)
            throw new ArgumentOutOfRangeException(nameof(exclusiveMaximum));
        ulong value;
        unchecked
        {
            state += 0x9E3779B97F4A7C15UL;
            value = state;
            value = (value ^ (value >> 30)) * 0xBF58476D1CE4E5B9UL;
            value = (value ^ (value >> 27)) * 0x94D049BB133111EBUL;
            value ^= value >> 31;
        }
        // Multiply-high bounded mapping; fixed work, no sampling retry loops.
        return (long)(((UInt128)value * (ulong)exclusiveMaximum) >> 64);
    }

    public int NextInt(int minimum, int exclusiveMaximum)
    {
        if (minimum >= exclusiveMaximum)
            throw new ArgumentOutOfRangeException(nameof(exclusiveMaximum));
        return (int)(minimum + NextInt64((long)exclusiveMaximum - minimum));
    }

    public void Shuffle<T>(IList<T> values)
    {
        for (var index = values.Count - 1; index > 0; index--)
        {
            var other = NextInt(0, index + 1);
            (values[index], values[other]) = (values[other], values[index]);
        }
    }
}
