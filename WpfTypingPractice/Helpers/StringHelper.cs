using System;
using System.Diagnostics.SymbolStore;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.International.Converters.PinYinConverter;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Ude;
using WpfTypingPractice.SimpleHelpers;

namespace WpfTypingPractice.Helpers
{
    public static class StringHelper
    {
        private static JObject pinyinDict;

        /// <summary>
        /// 当前的双拼方案
        /// </summary>
        public static ShuangpinScheme ShuangpinMode { get; set; } = ShuangpinScheme.MSShuangpin;

        /// <summary>
        /// 获取当前的双拼映射
        /// </summary>
        public static JObject GetShuangpinMap()
        {
            if (pinyinDict != null)
            {
                return (JObject)pinyinDict[ShuangpinMode.ToString().ToLower()]["map"];
            }
            return null;
        }

        static StringHelper()
        {
            // 读取外部的双拼键位映射
            var textReader = new StreamReader("pinyinKeys.json");
            var jsonReader = new JsonTextReader(textReader);
            pinyinDict = JObject.Load(jsonReader);
        }

        /// <summary>
        /// 判断一个字符是否是具有拼音的有效中文字符
        /// </summary>
        /// <param name="ch"></param>
        /// <returns></returns>
        public static bool IsValidChineseChar(char ch)
        {
            return ChineseChar.IsValidChar(ch);
        }

        /// <summary>
        /// 将一个拼音拆分为 (声母, 韵母)
        /// </summary>
        /// <param name="pinyin"></param>
        /// <returns></returns>
        public static (string consonant, string vowel) SplitConVow(string pinyin)
        {
            // 如果是双声母开头
            if (pinyin.StartsWithAny("zh", "ch", "sh"))
            {
                return (pinyin.Substring(0, 2), pinyin.Substring(2));
            }
            // 以一些韵母开头
            else if (pinyin.StartsWithAny("a", "e", "o"))
            {
                return ("", pinyin);
            }
            // 否则是单声母+韵母
            else
            {
                return (pinyin.Substring(0, 1), pinyin.Substring(1));
            }
        }

        /// <summary>
        /// 获取中文单字的拼音
        /// </summary>
        /// <param name="ch"></param>
        /// <returns>所有可能的拼音组成的字符串数组</returns>
        public static string[] GetPinyins(char ch)
        {
            if (!IsValidChineseChar(ch))
            {
                return null;
            }
            ChineseChar cc = new ChineseChar(ch);
            return cc.Pinyins.Where(p => !string.IsNullOrEmpty(p)).Select(p => p.ToLower().Substring(0, p.Length - 1)).ToArray();
        }

        /// <summary>
        /// 获取拼音对应的双拼
        /// </summary>
        /// <param name="pinyin"></param>
        /// <returns></returns>
        public static string GetShuangpin(string pinyin)
        {
            // 我们假定传入的拼音是正确有效的
            var convow = SplitConVow(pinyin);
            var con = convow.consonant;
            var vow = convow.vowel;

            var map = GetShuangpinMap();

            // 如果没有声母
            if (string.IsNullOrEmpty(con))
            {
                return map["vowelstart"].ToString() + GetShuangpinKey(vow);
            }

            // 表明是声母+韵母
            return GetShuangpinKey(con) + GetShuangpinKey(vow);
        }

        /// <summary>
        /// 获取单个音对应的双拼按键
        /// </summary>
        /// <param name="syllable"></param>
        /// <returns></returns>
        public static string GetShuangpinKey(string syllable)
        {
            if (syllable.Length == 1 && syllable != "v")
            {
                return syllable;
            }

            var map = GetShuangpinMap();
            // 遍历所有键位映射
            foreach (var pair in map)
            {
                if (pair.Value.Select(p => p.ToString()).Contains(syllable))
                {
                    return pair.Key;
                }
            }
            return null;
        }

        /// <summary>
        /// 检测一个外部文件的编码
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static Encoding GetEncoding(string path)
        {
            using (var fs = File.OpenRead(path))
            {
                CharsetDetector detector = new CharsetDetector();
                detector.Feed(fs);
                detector.DataEnd();
                if (!string.IsNullOrEmpty(detector.Charset))
                    return Encoding.GetEncoding(detector.Charset);
            }

            return null;
        }

        /// <summary>
        /// 判断一个字符串的开头是否符合一组字符串中的任意一个
        /// </summary>
        /// <param name="text"></param>
        /// <param name="patterns"></param>
        /// <returns></returns>
        public static bool StartsWithAny(this string text, params string[] patterns)
        {
            return patterns.Any(p => text.StartsWith(p));
        }

        /// <summary>
        /// 判断字符串是否与一组字符串中任意一个相同
        /// </summary>
        /// <param name="text"></param>
        /// <param name="others"></param>
        /// <returns></returns>
        public static bool EqualsAny(this string text, params string[] others)
        {
            foreach (var s in others)
            {
                if (text == s)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// 自动将文章的段落分隔更换为统一的间隔空行
        /// </summary>
        /// <param name="text"></param>
        /// <returns></returns>
        public static string AutoInsertEmptyLine(string text)
        {
            string linebreak = "\r\n";
            string newline = Environment.NewLine;
            if (!text.Contains(linebreak))
                linebreak = "\n";
            // 清除所有连续的换行符
            while (text.Contains(linebreak + linebreak))
            {
                text = text.Replace(linebreak + linebreak, linebreak);
            }
            return text.Replace(linebreak, newline + newline);
        }
    }
}
