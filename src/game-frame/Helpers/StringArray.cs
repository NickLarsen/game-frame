namespace GameFrame.Helpers
{
    public static class StringArrayHelpers
    {
        public static int IndexOf(this string[] source, string value)
        {
            for (int i = 0; i < source.Length; i++)
            {
                if (source[i] == value) return i;
            }
            return -1;
        }
    }
}
