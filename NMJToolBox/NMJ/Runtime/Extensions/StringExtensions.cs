namespace NMJToolBox
{
    public static class StringExtensions
    {
        public static string TrimStart(this string str, string wordToRemove)
        {
            if (str.StartsWith(wordToRemove))
            {
                str = str.Remove(0, wordToRemove.Length);
            }

            return str;
        }

        public static string TrimEnd(this string str, string wordToRemove)
        {
            if (str.EndsWith(wordToRemove))
            {
                str = str.Remove(str.Length - wordToRemove.Length, wordToRemove.Length);
            }

            return str;
        }
    }
}