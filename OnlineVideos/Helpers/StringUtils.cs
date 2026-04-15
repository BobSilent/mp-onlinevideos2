using System;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace OnlineVideos.Helpers
{
    public static class StringUtils
    {
        // Pre-compiled static patterns — paid once at class-load time, faster on every subsequent call.

        // ToFriendlyCase: insert space before each uppercase letter (except the first)
        private static readonly Regex _reFriendlyCase =
            new Regex("(?!^)([A-Z])", RegexOptions.Compiled);

        // ReplaceEscapedUnicodeCharacter: \uXXXX or %uXXXX sequences
        private static readonly Regex _reEscapedUnicode =
            new Regex(@"(?:\\|%)[uU]([0-9A-Fa-f]{4})", RegexOptions.Compiled);

        // PlainTextFromHtml: double-space collapse, <br/> variants, any HTML tag, repeated newlines
        private static readonly Regex _reDoubleSpace =
            new Regex(@"  +", RegexOptions.Multiline | RegexOptions.Compiled);
        private static readonly Regex _reBrTag =
            new Regex(@"< *br */*>", RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.Compiled);
        private static readonly Regex _reHtmlTag =
            new Regex(@"<[^>]*>", RegexOptions.Multiline | RegexOptions.Compiled);
        private static readonly Regex _reMultipleNewlines =
            new Regex(@"(\r?\n)+", RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.Compiled);

        // Tokenize: whitespace splitter used after token replacement
        private static readonly Regex _reWhitespace =
            new Regex(@"\s", RegexOptions.Compiled);

        public static string ToFriendlyCase(string PascalString)
        {
            return _reFriendlyCase.Replace(PascalString, " $1");
        }

        public static string ReplaceEscapedUnicodeCharacter(string input)
        {
            return _reEscapedUnicode.Replace(input,
                match => ((char)Int32.Parse(match.Value.Substring(2), NumberStyles.HexNumber)).ToString());
        }

        public static string GetRandomLetters(int amount)
        {
            var random = new Random();
            var sb = new StringBuilder(amount);
            for (int i = 0; i < amount; i++) sb.Append(Encoding.ASCII.GetString(new byte[] { (byte)random.Next('A', 'Z') }));
            return sb.ToString();
        }

        public static string[] Tokenize(string text, bool dropToken, params string[] tokens)
        {
            if (tokens.Length > 0)
            {

                string regex = @"([";
                foreach (string s in tokens)
                    regex += s;
                regex += "])";
                Regex RE = new Regex(regex);
                if (dropToken)
                {
                    string output = RE.Replace(text, " ");
                    return _reWhitespace.Split(output);
                }
                else
                    return (RE.Split(text));
            }
            return null;
        }

        public static string PlainTextFromHtml(string input)
        {
            string result = input;
            if (!string.IsNullOrEmpty(result))
            {
                // decode HTML escape character
                result = System.Web.HttpUtility.HtmlDecode(result);

                // Replace &nbsp; with space (plain string replace — no regex needed)
                result = result.Replace("&nbsp;", " ");

                // Remove double spaces
                result = _reDoubleSpace.Replace(result, "");

                // Replace <br/> with \n
                result = _reBrTag.Replace(result, "\n");

                // Remove remaining HTML tags
                result = _reHtmlTag.Replace(result, "");

                // Replace multiple newlines with just one
                result = _reMultipleNewlines.Replace(result, "\n");

                // Remove whitespace at the beginning and end
                result = result.Trim();
            }
            return result;
        }

        public static string GetSubString(string s, string start, string until)
        {
            if (string.IsNullOrEmpty(s)) return string.Empty;
            int p = s.IndexOf(start);
            if (p == -1) return String.Empty;
            p += start.Length;
            if (until == null) return s.Substring(p);
            int q = s.IndexOf(until, p);
            if (q == -1) return s.Substring(p);
            return s.Substring(p, q - p);
        }

        public static string GetRegExData(string regex, string data, string group = null)
        {
            string result = string.Empty;
            Match m = Regex.Match(data, regex);
            if (m.Success)
            {
                if (group == null)
                    result = m.Groups[1].Value;
                else
                    result = m.Groups[group].Value;
            }
            return result == null ? string.Empty : result;
        }

        private static string GetVal(string num, string[] pars)
        {
            int n = 0;
            for (int i = 0; i < num.Length; i++)
            {
                n = n * 36;
                char c = num[i];
                if (Char.IsDigit(c))
                    n += ((int)c) - 0x30;
                else
                    n += ((int)c) - 0x61 + 10;
            }
            if (n < 0 || n >= pars.Length)
                return n.ToString();

            return pars[n];
        }

        public static string UnPack(string packed)
        {
            int p = 2;
            while (p < packed.Length && !(packed[p] == '\'' && packed[p - 1] != '\\')) p++;
            //packed[p]=first non-escaped single quote

            string pattern = packed.Substring(0, p - 1).Replace(@"\'", @"'");
            p = packed.IndexOf('\'', p + 1);
            int q = packed.IndexOf('\'', p + 1);

            string[] pars = packed.Substring(p + 1, q - p - 1).Split('|');
            for (int i = 0; i < pars.Length; i++)
                if (String.IsNullOrEmpty(pars[i]))
                    if (i < 10)
                        pars[i] = i.ToString();
                    else
                        if (i < 36)
                        pars[i] = ((char)(i + 0x61 - 10)).ToString();
                    else
                        pars[i] = (i - 26).ToString();
            string res = String.Empty;
            string num = String.Empty;
            for (int i = 0; i < pattern.Length; i++)
            {
                char c = pattern[i];
                if (Char.IsDigit(c) || Char.IsLower(c))
                    num += c;
                else
                {
                    if (num.Length > 0)
                    {
                        res += GetVal(num, pars);
                        num = String.Empty;
                    }
                    res += c;
                }
            }
            if (num.Length > 0)
                res += GetVal(num, pars);

            return res;
        }

        private static string ToBase36(int i)
        {
            string chars = "0123456789abcdefghijklmnopqrstuvwxyz";
            string res = "";
            do
            {
                res += chars[i % 36];
                i = i / 36;
            } while (i > 0);
            return res;
        }

        private static string ToBase(int c, int a)
        {
            string res = (c < a ? "" : ToBase(c / a, a)) + ((c % a) > 35 ? ((char)(c % a + 29)).ToString() : ToBase36(c % a));
            return res;
        }

        public static string Unpack(string p, int a, int c, string[] k, int e, string d)
        {
            for (int i = c - 1; i >= 0; i--)
                if (i < k.Length && !String.IsNullOrEmpty(k[i]))
                    p = Regex.Replace(p, @"\b" + ToBase(i, a) + @"\b", k[i]);
            return p;
        }

    }
}
