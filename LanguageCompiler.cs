/*
MIT License

Copyright (c) 2022 Árnilsen Arthur Castilho Lopes

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
*/

using System;
using System.IO;
using System.Text;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Arnilsen.I18n
{
    /// <summary>
    /// Main language compiler class
    /// 
    /// THIS FILE CAN BE REMOVED FROM THE FINAL PROJECT
    /// </summary>
    public class LanguageCompiler
    {
        public const string PATTERN = "((\\#{)|(}\\#)|(\\#)|([\\{|\\}])|(\\\".*?\\\")|([A-Za-z0-9$_/]*))";

        public struct Token
        {
            public int type;
            public string value;
            public int line;
        }

        public struct SectionPosition
        {
            public ulong section;
            public int position;
            public int length;
        }

        public class Section
        {
            public ushort index = 0;
            public int pathDepth = 0;
            public string path = "";
            public SectionPosition position;

            #region Entries
            public Dictionary<ulong, string> entries = new Dictionary<ulong, string>();
            #endregion

            #region Section Tree
            public Section parent;
            public List<Section> child = new List<Section>();
            #endregion

            public override string ToString()
            {
                return ToString(0);
            }

            private string ToString(int depth)
            {
                string s = "";
                string pre = new String('\t', depth);
                s += $"{pre}{path}:\n";

                foreach(var i in child)
                    s += i.ToString(depth + 1);

                foreach(var i in entries)
                    s += $"{pre}- {i.Key}\n";

                return s;
            }

            /// <summary>
            /// Write section to bytes
            /// </summary>
            /// <param name="writer"></param>
            public void WriteToBytes(BinaryWriter writer)
            {
                SectionPosition position = new SectionPosition();
                position.position = (int) writer.BaseStream.Position;
                position.section = LanguageUtils.HashString(path);

                ushort entryCount = (ushort) entries.Count;
                ushort childCount = (ushort) child.Count;

                writer.Write(BitConverter.GetBytes(entryCount));

                foreach(var i in entries)
                {
                    //<8> <2> <n> 
                    writer.Write(BitConverter.GetBytes(i.Key));
                    byte[] bt = Encoding.UTF8.GetBytes(i.Value);
                    short length = (short) bt.Length;

                    writer.Write(BitConverter.GetBytes(length));
                    writer.Write(bt);
                }

                writer.Write(BitConverter.GetBytes(childCount));

                foreach(var i in child)
                {
                    //<2>
                    writer.Write(BitConverter.GetBytes(i.index));
                }     

                position.length = (int) writer.BaseStream.Position - position.position;
                this.position = position;
            }
        }

        public string file;
        
        /// <summary>
        /// Compile lang file to bytes
        /// </summary>
        /// <returns></returns>
        public byte[] Compile(string file)
        {
            #region Parsing State
            string[] path = new string[16];
            int depth = 0;
            #endregion
            
            #region Compilation State
            Dictionary<string, string> entries = new Dictionary<string, string>();
            List<Section> sections = new List<Section>();

            Section root = new Section();
            sections.Add(root);
            #endregion

            string currentKey = "";

            #region Parse File
            bool multilineComment = false;

            foreach(string s in File.ReadAllLines(file))
            {
                bool running = true;

                foreach(var token in ParseTokens(s))
                {
                    if(multilineComment)
                    {
                        if(token.type == 3)
                            multilineComment = false;
                        else
                            continue;
                    }

                    if(running != true)
                        break;

                    string value = token.value;

                    switch(token.type)
                    {
                        case 2:
                            multilineComment = true;
                            break;

                        case 4:                           
                            running = false;
                            break;

                        case 5:
                            if(token.value == "{")
                                depth ++;
                            else
                            {
                                depth --;

                                if(depth < 0)
                                    throw new Exception($"Attempted to close nonexistent  group at line {token.line}");
                            }
                            break;

                        case 6:
                            value = value.Substring(1, value.Length - 2);
                            if(entries.ContainsKey(currentKey))
                            {
                                entries[currentKey] = $"{entries[currentKey]}\n{value}";
                            }
                            else
                                entries.Add(currentKey, value);
                            break;

                        case 7:
                            bool isSection = value.StartsWith('$');
                            value = isSection ? value.Substring(1) : value;

                            path[depth] = value;

                            currentKey = BuildPath(path, depth);

                            if(isSection && !sections.Where((e) => e.path == currentKey).Any())
                            {
                                Section section = new Section();
                                section.path = currentKey;
                                section.pathDepth = 1 + currentKey.Count(f => (f == '/'));
                                sections.Add(section);
                            }

                            break;                               
                    }
                }
            }
            #endregion
            
            #region Sort Sections
            sections.Sort((a, b) => -a.pathDepth.CompareTo(b.pathDepth));
            #endregion

            #region Fill sections with entries
            foreach(var entry in entries)
            {
                foreach(var section in sections)
                {
                    if(entry.Key.StartsWith(section.path))
                    {
                        section.entries.Add(LanguageUtils.HashString(entry.Key), entry.Value);
                        break;
                    }
                }
            }
            #endregion

            sections.Reverse();

            #region Create Buffer
            List<Section> buffer = new List<Section>(sections.Count);

            for(int i = 0; i < sections.Count; i ++)
            {
                sections[i].index = (ushort) i;
                buffer.Add(sections[i]);
            }

            buffer.Remove(root);
            #endregion
            
            #region Section Tree Builder 
            BuildTree(root, buffer);
            #endregion

            #region Write sections
            using(MemoryStream stream = new MemoryStream())
            {
                using(BinaryWriter writer = new BinaryWriter(stream))
                {   
                    foreach(var section in sections)
                    {
                        section.WriteToBytes(writer);
                    }

                    foreach(var section in sections)
                    {
                        SectionPosition position = section.position;
                        writer.Write(BitConverter.GetBytes(position.section));
                        writer.Write(BitConverter.GetBytes(position.position));
                        writer.Write(BitConverter.GetBytes(position.length));
                        
                    }

                    ushort count = (ushort) sections.Count;
                    writer.Write(BitConverter.GetBytes(count));

                    return stream.ToArray();
                }
            }
            #endregion
        }

        /// <summary>
        /// Build section tree
        /// </summary>
        /// <param name="node"></param>
        /// <param name="others"></param>
        public void BuildTree(Section node, List<Section> others)
        {      
            int i = 0;
            while(i < others.Count)
            {
                var section = others[i];
                if(section.path.StartsWith(node.path))
                {
                    others.RemoveAt(i);

                    section.parent = node;
                    node.child.Add(section);

                    BuildTree(section, others);
                }
                else
                    i ++;
            }
        }

        /// <summary>
        /// Build path to string
        /// </summary>
        /// <param name="s"></param>
        /// <param name="current"></param>
        /// <returns></returns>
        public string BuildPath(string[] s,int current)
        {
            return string.Join('/', s, 0, current + 1);
        }

        /// <summary>
        /// Parse all tokens using RegEx
        /// </summary>
        /// /// <param name="content"></param>
        /// <returns></returns>
        public IEnumerable<Token> ParseTokens(string content)
        {
            Regex r = new Regex(PATTERN, RegexOptions.IgnoreCase);
            
            string[] lines = content.Split(
                new string[] { "\r\n", "\r", "\n" },
                StringSplitOptions.None
            );

            for(int a = 0; a < lines.Length; a ++)
            {
                Match m = r.Match(lines[a]);

                while (m.Success)
                {
                    for (int i = 2; i <= 7; i++)
                    {
                        Group g = m.Groups[i];

                        CaptureCollection cc = g.Captures;
                        for (int j = 0; j < cc.Count; j++)
                        {
                            Capture c = cc[j];
                            if(c.Length > 0)
                            {
                                yield return new Token{type = i, value = c.Value, line = a + 1};
                            }
                        }
                    }

                    m = m.NextMatch();
                }
            }
        }  
    }
}