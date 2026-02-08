namespace Boltzenberg.Functions.Algorithms.Tools
{
    public static class Permutations
    {
        public static IEnumerable<List<T>> Permute<T>(List<T> original)
        {
            return Permute(original, 0);
        }

        private static IEnumerable<List<T>> Permute<T>(List<T> src, int start)
        {
            if (start == src.Count - 1)
            {
                yield return new List<T>(src);
            }
            else
            {
                for (int i = start; i < src.Count; i++)
                {
                    Swap(src, start, i);

                    foreach (var perm in Permute(src, start + 1))
                    {
                        yield return perm;
                    }

                    Swap(src, start, i);
                }
            }
        }

        private static void Swap<T>(List<T> list, int x, int y)
        {
            T temp = list[x];
            list[x] = list[y];
            list[y] = temp;
        }
    }
}