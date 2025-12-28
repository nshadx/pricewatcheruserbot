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

        public async IAsyncEnumerable<T> Shuffle(int window)
        {
            await foreach (var chunk in source.Chunk(window))
            {
                Random.Shared.Shuffle(chunk);

                foreach (var item in chunk)
                {
                    yield return item;
                }
            }
        }
    }
}