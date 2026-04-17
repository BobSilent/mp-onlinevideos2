using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace OnlineVideos.MediaPortal1
{
    internal class SmsT9Filter
    {
        private Regex filter = null;
        private string numbers = String.Empty;

        private SortedSet<string> matches;

        public bool Matches(string name)
        {
            if (filter == null)
            {
                return true;
            }
            Match m = filter.Match(name);

            bool match = m.Success;
            while (m.Success)
            {
                matches.Add(m.Captures[0].Value.ToLowerInvariant());
                m = m.NextMatch();
            }

            return match;
        }

        public void Add(char c)
        {
            string currentPattern = filter == null ? string.Empty : filter.ToString();

            switch (c)
            {
                case '1': currentPattern += "[1]";         numbers += c; break;
                case '2': currentPattern += "[2|a|b|c]";   numbers += c; break;
                case '3': currentPattern += "[3|d|e|f]";   numbers += c; break;
                case '4': currentPattern += "[4|g|h|i]";   numbers += c; break;
                case '5': currentPattern += "[5|j|k|l]";   numbers += c; break;
                case '6': currentPattern += "[6|m|n|o]";   numbers += c; break;
                case '7': currentPattern += "[7|p|q|r|s]"; numbers += c; break;
                case '8': currentPattern += "[8|t|u|v]";   numbers += c; break;
                case '9': currentPattern += "[9|w|x|y|z]"; numbers += c; break;
                case '0': currentPattern += "[0|\\s]";     numbers += c; break;
                case '\b':
                    if (!string.IsNullOrEmpty(currentPattern))
                    {
                        numbers = numbers.Substring(0, numbers.Length - 1);
                        currentPattern = currentPattern.Substring(0, currentPattern.LastIndexOf('['));
                    }
                    break;
            }
            if (string.IsNullOrEmpty(currentPattern))
            {
                filter = null;
            }
            else
            {
                filter = new Regex(currentPattern, RegexOptions.IgnoreCase);
            }
        }

        public void Clear()
        {
            filter = null;
            numbers = string.Empty;
        }

        public bool IsEmpty()
        {
            return filter == null;
        }

        public void StartMatching()
        {
            matches = new SortedSet<string>();
        }

        public override string ToString()
        {
            if (filter == null)
            {
                return numbers;
            }
            return $"{numbers} {{{string.Join(",", matches)}}}";
        }
    }
}
