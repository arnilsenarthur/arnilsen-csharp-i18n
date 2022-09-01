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
using System.Security.Cryptography;

namespace Arnilsen.I18n
{
    public class LanguageUtils
    {
        /// <summary>
        /// Hash 'string' to 'ulong'
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static ulong HashString(string text)
        {
            using (HashAlgorithm hashAlgorithm = SHA256.Create())
            {
                var bytes = hashAlgorithm.ComputeHash(Encoding.Default.GetBytes(text));
                Array.Resize(ref bytes, bytes.Length + bytes.Length % 8);
                return Enumerable.Range(0, bytes.Length / 8)
                    .Select(i => BitConverter.ToUInt64(bytes, i * 8))
                    .Aggregate((x, y) =>x ^ y);
            }
        }

        /// <summary>
        /// Hash 'string[]' to 'ulong[]'
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static ulong[] HashStrings(string[] text)
        {
            ulong[] hashs = new ulong[text.Length];
            for(int i = 0; i < text.Length; i ++)
                hashs[i] = HashString(text[i]);

            return hashs;
        }
    }
}