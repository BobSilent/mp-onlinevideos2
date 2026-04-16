using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;

namespace OnlineVideos.Helpers
{
    public static class CollectionUtils
    {
        private static readonly XmlWriterSettings _settings = new XmlWriterSettings()
        {
            Encoding = Encoding.UTF8,
            Indent = true,
            OmitXmlDeclaration = true
        };

        public static string DictionaryToString(Dictionary<string, string> dic)
        {
            var sb = new StringBuilder();
            using (XmlWriter writer = XmlWriter.Create(sb, _settings))
            {
                writer.WriteStartElement("dictionary");
                foreach (string key in dic.Keys)
                {
                    writer.WriteStartElement("item");
                    writer.WriteStartElement("key");
                    writer.WriteCData(key);
                    writer.WriteEndElement();
                    writer.WriteStartElement("value");
                    writer.WriteCData(dic[key]);
                    writer.WriteEndElement();
                    writer.WriteEndElement();
                }
                writer.WriteEndElement();
                writer.Flush();
            }
            return sb.ToString();
        }

        public static Dictionary<string, string> DictionaryFromString(string input)
        {
            Dictionary<string, string> dic = new Dictionary<string, string>();
            using (XmlReader reader = XmlReader.Create(new StringReader(input)))
            {
                bool wasEmpty = reader.IsEmptyElement;
                reader.Read();
                if (wasEmpty)
                {
                    return null;
                }

                reader.ReadStartElement("dictionary");
                while (reader.NodeType != XmlNodeType.EndElement)
                {
                    reader.ReadStartElement("item");
                    reader.ReadStartElement("key");
                    string key = reader.ReadContentAsString();
                    reader.ReadEndElement();
                    reader.ReadStartElement("value");
                    string value = reader.ReadContentAsString();
                    reader.ReadEndElement();
                    dic.Add(key, value);
                    reader.ReadEndElement();
                    reader.MoveToContent();
                }
                reader.ReadEndElement();
            }
            return dic;
        }

        // Shared instance avoids duplicate shuffle sequences when Randomize is called
        // multiple times within the same clock tick (new Random() seeds from the system clock).
        private static readonly Random _rng = new Random();

        public static void Randomize<T>(this List<T> list)
        {
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = _rng.Next(n + 1);
                // swap list[n] and list[k]
                (list[n], list[k]) = (list[k], list[n]);
            }
        }
    }
}
