using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SpeedDate.Configuration
{
    internal class XmlParser : IDisposable
    {
        public TextReader Stream { get; private set; }
        public string Text { get; private set; }
        public string Tag { get; private set; }

        public XmlParser(TextReader stream) { Stream = stream; }
        public XmlParser(string source) : this(new StringReader(source)) { }

        public void Dispose()
        {
            Stream?.Dispose();
            Stream = null;
        }

        private readonly Dictionary<string, string> _values = new Dictionary<string, string>();

        public string this[string key] => _values.TryGetValue(key, out var ret) ? ret : null;

        private string _reserved;

        public bool Read()
        {
            Text = Tag = null;
            _values.Clear();
            if (Stream == null) return false;

            if (_reserved != null)
            {
                Tag = _reserved;
                _reserved = null;
            }
            else
                ReadText();
            return true;
        }
        public void Search(string tag, Action a)
        {
            while (Read())
            {
                if (Tag != tag)
                    continue;

                a();
                break;
            }
        }

        public void Search(string tag, Func<bool> f, Action a)
        {
            while (Read())
            {
                if (Tag != tag || !f())
                    continue;

                a();
                break;
            }
        }
        public void SearchEach(string tag, Action a)
        {
            var end = "/" + Tag;
            while (Read())
            {
                if (Tag == end)
                    break;
                if (Tag == tag)
                    a();
            }
        }

        public void SearchEach(string tag, Func<bool> f, Action a)
        {
            var end = "/" + Tag;
            while (Read())
            {
                if (Tag == end)
                    break;
                if (Tag == tag && f())
                    a();
            }
        }

        private int _current;

        private int ReadChar()
        {
            if (Stream == null) return _current = -1;
            _current = Stream.Read();
            if (_current == -1) Dispose();
            return _current;
        }

        private void ReadText()
        {
            var text = new StringBuilder();
            int ch;
            while ((ch = ReadChar()) != -1)
            {
                if (ch == '<') break;
                text.Append((char)ch);
            }
            Text = FromEntity(text.ToString());
            if (ch == '<') ReadTag();
        }

        private void ReadTag()
        {
            int ch;
            var tag = new StringBuilder();
            while ((ch = ReadChar()) != -1)
            {
                if (ch == '>' || (ch == '/' && tag.Length > 0))
                    break;
                else if (ch > ' ')
                {
                    tag.Append((char)ch);
                    if (tag.Length == 3 && tag.ToString() == "!--") break;
                }
                else if (tag.Length > 0)
                    break;
            }
            Tag = tag.ToString();
            if (ch == '/')
            {
                _reserved = "/" + Tag;
                ch = ReadChar();
            }
            if (ch != '>')
            {
                if (Tag == "!--")
                    ReadComment();
                else
                    while (ReadAttribute()) ;
            }
        }

        private void ReadComment()
        {
            int ch, m = 0;
            var comment = new StringBuilder();
            while ((ch = ReadChar()) != -1)
            {
                if (ch == '>' && m >= 2)
                {
                    comment.Length -= 2;
                    break;
                }
                comment.Append((char)ch);
                if (ch == '-') m++; else m = 0;
            }
            _values["comment"] = comment.ToString();
        }

        private bool ReadAttribute()
        {
            string name = null;
            do
            {
                var n = ReadValue();
                switch (_current)
                {
                    case '>':
                        return false;
                    case '/':
                        _reserved = "/" + Tag;
                        break;
                }
                if (n != "") name = n;
            } while (_current != '=');
            var value = ReadValue();
            if (name != null) _values[name] = value;
            return _current != '>';
        }

        private string ReadValue()
        {
            int ch;
            var value = new StringBuilder();
            while ((ch = ReadChar()) != -1)
            {
                if (ch == '>' || ch == '=' || ch == '/')
                    break;
                else if (ch == '"')
                {
                    while ((ch = ReadChar()) != -1)
                    {
                        if (ch == '"') break;
                        value.Append((char)ch);
                    }
                    break;
                }
                else if (ch > ' ')
                    value.Append((char)ch);
                else if (value.Length > 0)
                    break;
            }
            return value.ToString();
        }

        public static string FromEntity(string s)
        {
            return s
                .Replace("&lt;", "<")
                .Replace("&gt;", ">")
                .Replace("&quot;", "\"")
                .Replace("&nbsp;", " ")
                .Replace("&amp;", "&");
        }
    }
}