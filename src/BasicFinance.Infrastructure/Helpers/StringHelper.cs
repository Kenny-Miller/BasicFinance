using System.Text;

namespace BasicFinance.Infrastructure.Helpers
{
    public static class StringHelper
    {
        /// <summary>
        /// Trims and compresses multiple consecutive whitespace characters in the input string into a single space.
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static string NormalizeWhiteSpace(string str)
        {
            var strBuilder = new StringBuilder(str);
            var prevIsWhiteSpace = false;
            foreach (var c in str)
            {
                if (Char.IsWhiteSpace(c) && !prevIsWhiteSpace)
                {
                    prevIsWhiteSpace = true;
                    strBuilder.Append(' ');
                }
                else if (!Char.IsWhiteSpace(c))
                {
                    prevIsWhiteSpace = false;
                    strBuilder.Append(c);
                }
            }

            return strBuilder.ToString();
        }
    }
}
