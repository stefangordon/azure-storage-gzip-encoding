using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ASGE
{
    public static class EnumerableExtensions
    {
        /// <summary>
        /// From https://devblogs.microsoft.com/pfxteam/implementing-a-simple-foreachasync-part-2/.
        /// </summary>
        public static Task ForEachAsync<T>(this IEnumerable<T> source, Func<T, Task> body, int dop = 4)
        {
            return Task.WhenAll(
                from partition in Partitioner.Create(source).GetPartitions(dop)
                select Task.Run(async delegate
                {
                    using (partition)
                        while (partition.MoveNext())
                            await body(partition.Current);
                }));
        }
    }
}
