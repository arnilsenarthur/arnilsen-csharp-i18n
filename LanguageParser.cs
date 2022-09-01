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
using System.Text;
using System.Linq;
using System.Collections.Generic;

namespace Arnilsen.I18n
{
    /// <summary>
    /// Language parser base class
    /// </summary>
    public abstract class LanguageParser
    {
        public struct SectionSnapshot
        {
            public ulong section;
            public int position;
            public int length;
        }

        public class Section
        {
            public ulong id;
            public Dictionary<ulong, string> entries = new Dictionary<ulong, string>();
            public List<Section> children = new List<Section>();
        }

        public List<Section> sections = new List<Section>();
        public SectionSnapshot[] snapshots;

        /// <summary>
        /// Get total byte count
        /// </summary>
        /// <returns></returns>
        public virtual int GetByteCount()
        {
            return 0;
        }
        
        /// <summary>
        /// Get byte[] segment
        /// </summary>
        /// <param name="position"></param>
        /// <param name="length"></param>
        /// <returns></returns>
        public virtual ArraySegment<byte> GetBytes(int position, int length)
        {
            return null;
        }
        
        public Random random = new Random();

        /// <summary>
        /// Parse bytes to sections
        /// </summary>
        /// <param name="bytes"></param>
        public void Parse()
        {
            int byteCount = GetByteCount();

            ArraySegment<byte> bytes = GetBytes(byteCount - 2, 2);

            ushort length = BitConverter.ToUInt16(bytes.Array, bytes.Offset);

            //Load section snapshots
            snapshots = new SectionSnapshot[length];

            bytes = GetBytes(byteCount - length * 16 - 2, length * 16);
            
            for(int i = 0; i < length; i ++)
            {           
                ulong section = BitConverter.ToUInt64(bytes.Array, bytes.Offset + i * 16);
                int position = BitConverter.ToInt32(bytes.Array, bytes.Offset + i * 16 + 8);
                int l = BitConverter.ToInt32(bytes.Array, bytes.Offset + i * 16 + 12);
                snapshots[i] = new SectionSnapshot{position = position, section = section, length = l};
            }
        }

        /// <summary>
        /// Get entry replacing args
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public string GetEntry(string key, params object[] args) => string.Format(GetEntry(LanguageUtils.HashString(key)), args);

        /// <summary>
        /// Get entry from loaded sections
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public string GetEntry(string key) => GetEntry(LanguageUtils.HashString(key));

        /// <summary>
        /// Get entry replacing args
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public string GetEntry(ulong key, params object[] args) => string.Format(GetEntry(key), args);

        /// <summary>
        /// Get entry from loaded sections
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public string GetEntry(ulong key)
        {
            foreach(var section in sections)
            {
                var v = section.entries.GetValueOrDefault(key, null);
                
                if(v != null)
                {
                    if(v.Contains('\n'))
                    {
                        string[] entries = v.Split('\n');
                        return entries[random.Next(entries.Length)];
                    }

                    return v;
                }
            }

            return "?";
        }

        /// <summary>
        /// Load multiple sections at the same time
        /// </summary>
        /// <param name="sections"></param>
        /// <returns></returns>
        public Section[] LoadSections(params string[] sections)
        {
            Section[] sec = new Section[sections.Length];

            for(int i = 0; i < sections.Length; i ++)
                sec[i] = LoadSection(sections[i], new ulong[0]);
            
            return sec;
        }

        /// <summary>
        /// Load multiple sections at the same time
        /// </summary>
        /// <param name="sections"></param>
        /// <param name="ignore"></param>
        /// <returns></returns>
        public Section[] LoadSections(string[] sections, string[] ignore)
        {
            ulong[] ignoreIds = LanguageUtils.HashStrings(ignore);

            Section[] sec = new Section[sections.Length];

            for(int i = 0; i < sections.Length; i ++)
                sec[i] = LoadSection(sections[i], ignoreIds);
            
            return sec;
        }

        /// <summary>
        /// Load a section
        /// </summary>
        /// <param name="section"></param>
        /// <returns></returns>
        public Section LoadSection(string section)
        {
            return LoadSection(section, new ulong[0]);
        }

        /// <summary>
        /// Load a section ignoring some sub-sections
        /// </summary>
        /// <param name="section"></param>
        /// <param name="ignore"></param>
        /// <returns></returns>
        public Section LoadSection(string section, params string[] ignore)
        {
            ulong[] ignoreIds = LanguageUtils.HashStrings(ignore);
            return LoadSection(section, ignoreIds);
        }

        /// <summary>
        /// Load a section ignoring some sub-sections
        /// </summary>
        /// <param name="section"></param>
        /// <param name="ignore"></param>
        /// <returns></returns>
        public Section LoadSection(string section, params ulong[] ignore)
        {
            ulong hash = LanguageUtils.HashString(section);

            var search = snapshots.Where((s) => s.section == hash);

            if(search.Count() == 0)
                return null;

            SectionSnapshot snapshot = search.First();

            return LoadSection(snapshot, ignore);
        }
        
        private Section LoadSection(SectionSnapshot snapshot, ulong[] ignore, Section parent = null)
        {
            #region Load Header
            ArraySegment<byte> bytes = GetBytes(snapshot.position, snapshot.length);
            int position = bytes.Offset;

            Section section = new Section();
            section.id = snapshot.section;
            sections.Add(section);
            #endregion
            
            #region Load Entries
            ushort entryCount = BitConverter.ToUInt16(bytes.Array, position);
            position += 2;

            //Add to parent
            if(parent != null)
                parent.children.Add(section);

            //<8> <2> <n> 
            for(int i = 0; i < entryCount; i ++)
            {
                ulong id = BitConverter.ToUInt64(bytes.Array, position);
                position += 8;

                short length = BitConverter.ToInt16(bytes.Array, position);
                position += 2;

                string s = Encoding.UTF8.GetString(bytes.Array, position, length);
                position += length;

                section.entries[id] = s;
            }
            #endregion
            
            #region Load Children
            ushort childCount = BitConverter.ToUInt16(bytes.Array, position);
            position += 2;

            for(int i = 0; i < childCount; i ++)
            {
                ushort index = BitConverter.ToUInt16(bytes.Array, position);
                position += 2;

                SectionSnapshot ss = snapshots[index];

                if(!ignore.Contains(ss.section))
                    LoadSection(ss, ignore, section);
            }
            #endregion

            return section;
        }

        /// <summary>
        /// Unload multiples sections at the same time
        /// </summary>
        /// <param name="section"></param>
        public void UnloadSections(string[] section, params string[] ignore)
        {
            ulong[] ignoreIds = LanguageUtils.HashStrings(ignore);

            foreach(string s in section)
                UnloadSection(s, ignoreIds);
        }

        /// <summary>
        /// Unload multiples sections at the same time
        /// </summary>
        /// <param name="section"></param>
        public void UnloadSections(string[] section, params ulong[] ignore)
        {
            foreach(string s in section)
                UnloadSection(s, ignore);
        }

        /// <summary>
        /// Unload a section
        /// </summary>
        /// <param name="section"></param>
        public void UnloadSection(string section, params ulong[] ignore)
        {
            ulong id = LanguageUtils.HashString(section);
            var s = sections.Where((sec) => sec.id == id).FirstOrDefault();
            
            if(s != null)
                UnloadSection(s, ignore);
        }

        /// <summary>
        /// Unload a section
        /// </summary>
        /// <param name="section"></param>
        public void UnloadSection(string section, params string[] ignore)
        {
            ulong id = LanguageUtils.HashString(section);
            var s = sections.Where((sec) => sec.id == id).FirstOrDefault();
            
            if(s != null)
                UnloadSection(s, ignore);
        }

        /// <summary>
        /// Unload a section
        /// </summary>
        /// <param name="section"></param>
        public void UnloadSection(Section section)
        {
            sections.Remove(section);

            foreach(var child in section.children)
            {
                UnloadSection(child);
            }
        }

        /// <summary>
        /// Unload a section
        /// </summary>
        /// <param name="section"></param>
        public void UnloadSection(Section section, params ulong[] ignore)
        {
            sections.Remove(section);

            foreach(var child in section.children)
            {
                if(!ignore.Contains(child.id))
                    UnloadSection(child, ignore);
            }
        }

        /// <summary>
        /// Unload a section
        /// </summary>
        /// <param name="section"></param>
        public void UnloadSection(Section section, params string[] ignore)
        {
            ulong[] ignoreIds = LanguageUtils.HashStrings(ignore);
            UnloadSection(section, ignoreIds);
        }

        /// <summary>
        /// Check if a section is loaded
        /// </summary>
        /// <param name="section"></param>
        /// <returns></returns>
        public bool IsSectionLoaded(string section)
        {
            return IsSectionLoaded(LanguageUtils.HashString(section));
        }

        /// <summary>
        /// Check if a section is loaded
        /// </summary>
        /// <param name="section"></param>
        /// <returns></returns>
        public bool IsSectionLoaded(ulong section)
        {
            foreach(var s in sections)
                if(s.id == section)
                    return true;

            return false;
        }       
    }
}
