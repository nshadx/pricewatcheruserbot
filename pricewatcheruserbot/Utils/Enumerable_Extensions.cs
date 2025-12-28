namespace pricewatcheruserbot.Utils;

public static class Enumerable_Extensions
{
    extension<T>(IAsyncEnumerable<T> source)
    {
        public IAsyncEnumerable<T> Merge(IEnumerable<IAsyncEnumerable<T>> parts)
        {
            foreach (var part in parts)
            {
                source = source.Concat(part);
            }

            return source;
        }

        public async IAsyncEnumerable<T> Randomize(int window = 10)
        {
            var container = new bool[window];
        
            await foreach (var chunk in source.Chunk(window))
            {
                var max = Math.Min(chunk.Length, window);
                int index;
                do
                {
                    index = Random.Shared.Next(max);
                } while (container[index]);
                
                yield return chunk[index];
            }
        }
    }
}