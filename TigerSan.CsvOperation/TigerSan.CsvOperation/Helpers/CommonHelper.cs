using System.IO;
using System.Threading.Tasks;
using TigerSan.CsvOperation.Models;

namespace TigerSan.CsvOperation.Helpers
{
    public static class CommonHelper
    {
        #region 异步读取“所有行”
        public static async Task<string[]> ReadAllLinesAsync(string path)
        {
            string[] lines = { };

            if (!File.Exists(path)) return lines;

            await Task.Run(() =>
            {
                lines = File.ReadAllLines(path);
            });

            return lines;
        }
        #endregion

        #region 异步读取“所有文本”
        public static async Task<string> ReadAllTextAsync(string path)
        {
            if (!File.Exists(path)) return string.Empty;

            string lines = string.Empty;

            await Task.Run(() =>
            {
                lines = File.ReadAllText(path);
            });

            return lines;
        }
        #endregion

        #region 异步写入“所有文本”
        public static async Task WriteAllTextAsync(string path, string str)
        {
            await Task.Run(() =>
            {
                File.WriteAllText(path, str);
            });
        }
        #endregion

        #region 获取“指定字符”的个数
        public static int GetCharCount(string str, char ch)
        {
            int count = 0;
            foreach (var c in str)
            {
                if (Equals(c, ch))
                {
                    ++count;
                }
            }
            return count;
        }
        #endregion

        #region 获取“无换行符”字符串
        public static string GetNoNewLineString(string value)
        {
            return value.Replace("\r\n", " ").Replace("\n", " ").Replace("\r", " ");
        }
        #endregion

        #region 判断“指定字符”是否“成对出现”
        public static bool IsPaired(string input, char ch)
        {
            int i = 0;
            int n = input.Length;

            while (i < n)
            {
                if (input[i] == '"')
                {
                    // 检查是否紧跟另一个双引号：
                    if (i + 1 < n && input[i + 1] == '"')
                    {
                        i += 2; // 跳过这对连续的双引号
                    }
                    else
                    {
                        return false; // 发现未配对的单引号
                    }
                }
                else
                {
                    i++;
                }
            }

            return true; // 所有双引号都成对出现
        }
        #endregion

        #region 移除“字符串”的“首尾字符”
        public static string RemoveFirstAndLastChar(string str)
        {
            if (str.Length < 2)
            {
                return string.Empty;
            }
            return str.Substring(1, str.Length - 2);
        }
        #endregion

        #region 判断“源数据”是否合法
        public static CsvResult IsSourceVerifyOk(string value)
        {
            if (value.Contains("\n")) // 不能包含换行符
            {
                return new CsvResult(CsvResultType.Warning, "The value cannot contain a newline character!");
            }
            else if (value.StartsWith("\"")) // 引号必须闭合
            {
                var Quotation_Not_Paired = new CsvResult(CsvResultType.Error, $"The quotation is not paired!");

                // 引号未闭合：
                if (value.Length < 2 || !value.EndsWith("\""))
                {
                    return Quotation_Not_Paired;
                }

                // 移除首尾引号：
                var strSrc = CommonHelper.RemoveFirstAndLastChar(value);

                // 引号未成对出现：
                if (!CommonHelper.IsPaired(strSrc, '\"'))
                {
                    return Quotation_Not_Paired;
                }
            }

            return new CsvResult();
        }
        #endregion
    }
}
