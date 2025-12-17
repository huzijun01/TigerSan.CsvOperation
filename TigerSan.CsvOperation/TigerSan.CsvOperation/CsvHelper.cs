using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using TigerSan.CsvOperation.Models;
using TigerSan.CsvOperation.Helpers;

namespace TigerSan.CsvOperation
{
    public class CsvHelper
    {
        #region 【Fields】
        private CsvResult File_Not_Exist { get => new CsvResult(CsvResultType.Error, $"The file does not exist!{Environment.NewLine}{_path}"); }

        /// <summary>
        /// 文件路径
        /// </summary>
        public readonly string _path;

        /// <summary>
        /// 内边距
        /// </summary>
        public readonly int _padding = 2;

        /// <summary>
        /// 时间戳格式
        /// </summary>
        public string _dateTimeFormat = "yyyy-MM-dd HH:mm:ss";
        #endregion 【Fields】

        #region 【Properties】
        /// <summary>
        /// 行长度
        /// </summary>
        public int RowLength { get; private set; }

        /// <summary>
        /// 列头集合
        /// </summary>
        public List<CsvHeader> Headers { get; set; } = new List<CsvHeader>();

        /// <summary>
        /// 行集合
        /// </summary>
        public List<CsvRow> Rows { get; set; } = new List<CsvRow>();
        #endregion 【Properties】

        #region 【Ctor】
        public CsvHelper(string path)
        {
            _path = path;
        }
        #endregion 【Ctor】

        #region 【Functions】
        #region [Private]
        #region 更新“长度”
        private CsvResult UpdateLength()
        {
            var res = IsVerifyOk();

            if (!res.IsSuccess) return res;

            #region 计算“列最大长度”
            for (int iCol = 0; iCol < Headers.Count; iCol++)
            {
                var header = Headers[iCol];
                header.MaxLength = header.Target.Length;

                for (int iRow = 0; iRow < Rows.Count; iRow++)
                {
                    var row = Rows[iRow];
                    var item = row.Items[iCol];

                    if (item.Target.Length > header.MaxLength)
                    {
                        header.MaxLength = item.Target.Length;
                    }
                }
            }
            #endregion 计算“列最大长度”

            #region 计算“行长度”
            RowLength = 0;

            for (int iCol = 0; iCol < Headers.Count; iCol++)
            {
                var header = Headers[iCol];

                RowLength += header.MaxLength;
            }
            #endregion 计算“行长度”

            return res;
        }
        #endregion

        #region 获取“项目基类”集合
        private GetItemBasesResult GetItemBases(string line)
        {
            // 字符数组：
            var chars = line.ToCharArray();
            // “项目基类”集合：
            var itemBases = new List<CsvItemBase>();
            // 结果：
            var basesResult = new GetItemBasesResult(itemBases);
            // 临时源数据：
            var sbTempSource = new StringBuilder();
            // 当前项目：
            var itemCurrent = new CsvItemBase();
            itemBases.Add(itemCurrent);

            // 设置数据：
            for (int iCurrent = 0; iCurrent < chars.Length; iCurrent++)
            {
                var ch = chars[iCurrent];

                if (Equals(ch, '"')) // 引号开头
                {
                    // 引号结束位置：
                    var iQuotEnd = iCurrent;

                    #region 获取“引号结束位置”
                    while (true)
                    {
                        ++iQuotEnd;

                        if (iQuotEnd >= chars.Length) // 已到达行尾
                        {
                            iQuotEnd = chars.Length - 1; // 回退到最后一个字符位置

                            // “源数据”字符串：
                            var strSource = line.Substring(iCurrent, iQuotEnd - iCurrent + 1);

                            // 验证“源数据”字符串：
                            var res = CommonHelper.IsSourceVerifyOk(strSource);
                            switch (res.Type)
                            {
                                case CsvResultType.Success:
                                    break;
                                case CsvResultType.Warning:
                                    basesResult.Type = CsvResultType.Warning;
                                    basesResult.Message += $"{res.Message}{Environment.NewLine}";
                                    break;
                                case CsvResultType.Error:
                                    var sb = new StringBuilder();
                                    sb.AppendLine(res.Message);
                                    sb.AppendLine($"Line: {line}");
                                    sb.AppendLine($"Position: {iQuotEnd}");
                                    return new GetItemBasesResult(null, CsvResultType.Error, sb.ToString());
                                default:
                                    break;
                            }

                            break;
                        }
                        else if (Equals(chars[iQuotEnd], ',')
                            && Equals(chars[iQuotEnd - 1], '"')
                            && !Equals(iQuotEnd - 1, iCurrent)) // 逗号结束
                        {
                            --iQuotEnd; // 回退到“引号位置”
                            break;
                        }
                        else if (Equals(chars[iQuotEnd], '"')) // 内容中引号必须成对出现
                        {
                            if (Equals(iQuotEnd, iCurrent)) // 跳过起始引号
                            {
                                continue;
                            }
                            else if (iQuotEnd < chars.Length - 2) // 后面还有字符
                            {
                                if (Equals(chars[iQuotEnd + 1], ',')) // 跳过结束引号
                                {
                                    continue;
                                }
                                else if (!Equals(chars[iQuotEnd + 1], '"')) // 引号未成对出现
                                {
                                    var sb = new StringBuilder();
                                    sb.AppendLine($"The quotation is not paired!");
                                    sb.AppendLine($"Line: {line}");
                                    sb.AppendLine($"Position: {iQuotEnd}");
                                    return new GetItemBasesResult(null, CsvResultType.Error, sb.ToString());
                                }
                            }

                            ++iQuotEnd; // 跳过一对冒号中的第二个引号
                        }
                    }
                    #endregion 获取“引号结束位置”

                    #region 复制“项目字符串”
                    for (int iCopy = iCurrent; iCopy <= iQuotEnd; iCopy++)
                    {
                        sbTempSource.Append(chars[iCopy]);
                    }
                    #endregion 复制“项目字符串”

                    iCurrent = iQuotEnd + 1; // 跳过“逗号”
                }
                else // 普通字符
                {
                    int iCopy = iCurrent;

                    #region 复制“项目字符串”
                    for (; iCopy < chars.Length; iCopy++)
                    {
                        iCurrent = iCopy;
                        var chCopy = chars[iCurrent];

                        if (Equals(chCopy, '"')) // 不应出现引号
                        {
                            var sb = new StringBuilder();
                            sb.AppendLine($"The quotation is not expected here!");
                            sb.AppendLine($"Line: {line}");
                            sb.AppendLine($"Position: {iCurrent}");
                            return new GetItemBasesResult(null, CsvResultType.Error, sb.ToString());
                        }
                        else if (Equals(chCopy, ',')) // 逗号结束
                        {
                            break;
                        }
                        else // 复制字符
                        {
                            sbTempSource.Append(chCopy);
                        }
                    }
                    #endregion 复制“项目字符串”
                }

                #region 下一个项目
                if (iCurrent < chars.Length && Equals(chars[iCurrent], ',')) // 逗号结束
                {
                    // 设置“源数据”：
                    itemCurrent.Source = sbTempSource.ToString();
                    // 添加“新项目”：
                    itemCurrent = new CsvItemBase();
                    itemBases.Add(itemCurrent);
                    // 清空“临时源数据”：
                    sbTempSource.Clear();
                }
                #endregion 下一个项目
            }

            // 设置“源数据”：
            itemCurrent.Source = sbTempSource.ToString();

            return basesResult;
        }
        #endregion

        #region 判断“是否验证无误”
        private CsvResult IsVerifyOk()
        {
            for (int iRow = 0; iRow < Rows.Count; iRow++)
            {
                var row = Rows[iRow];

                // 每行项目个数相同：
                if (row.Items.Count != Headers.Count)
                {
                    var sb = new StringBuilder();
                    sb.AppendLine($"The item count is not equal to header count!");
                    sb.AppendLine($"Row: {iRow + 1}");
                    sb.AppendLine($"ItemCount: {row.Items.Count}");
                    sb.AppendLine($"HeaderCount: {Headers.Count}");

                    return new CsvResult(CsvResultType.Error, sb.ToString());
                }
            }

            return new CsvResult();
        }
        #endregion

        #region 将“对象”设置到“数据行”
        private CsvResult SetObjectToRow(PropertyInfo[] properties, object obj)
        {
            var res = new CsvResult();
            var row = new CsvRow(this);
            Rows.Add(row);

            // 遍历对象属性：
            foreach (var prop in properties)
            {
                var item = new CsvItem(row);
                var value = prop.GetValue(obj);
                var propType = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;
                row.Items.Add(item);

                try
                {
                    if (propType != typeof(string) && value != null) // 处理值类型转换
                    {
                        if (propType == typeof(DateTime)) // 特殊处理DateTime格式
                        {
                            value = ((DateTime)value).ToString(_dateTimeFormat);
                        }
                        else // 通用类型转换
                        {
                            value = Convert.ChangeType(value, typeof(string));
                        }
                    }
                    else if (value == null || Convert.IsDBNull(value)) // 处理DBNull和null
                    {
                        value = string.Empty;
                    }
                }
                catch (Exception ex)
                {
                    res.Type = CsvResultType.Warning;
                    res.Message += $"Property '{prop.Name}' conversion failed: {ex.Message}{Environment.NewLine}";
                }

                item.Target = value.ToString();
            }

            return res;
        }
        #endregion

        #region 将“数据行”设置到“对象”
        private CsvResult SetRowToObject<T>(PropertyInfo[] properties, CsvRow row, T obj) where T : class, new()
        {
            // “列头”集合：
            var headers = row._model.Headers;

            // 验证：
            var res = IsVerifyOk();
            if (!res.IsSuccess) return res;

            // 逐列设置属性值：
            for (int iCol = 0; iCol < headers.Count; iCol++)
            {
                var columnName = headers[iCol].Target;
                var property = properties.FirstOrDefault(p => p.Name.Equals(columnName, StringComparison.OrdinalIgnoreCase));

                if (property == null)
                {
                    res.Type = CsvResultType.Warning;
                    res.Message += $"The property '{columnName}' is not found in type '{typeof(T).FullName}'.{Environment.NewLine}";
                    continue;
                }

                try
                {
                    var item = row.Items[iCol];
                    object convertedValue = null;

                    var type = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
                    convertedValue = Convert.ChangeType(item.Target, type);

                    property.SetValue(obj, convertedValue);
                }
                catch (Exception e)
                {
                    res.Type = CsvResultType.Warning;
                    res.Message += $"Failed to set property '{property.Name}' with value '{row.Items[iCol].Target}': {e.Message}{Environment.NewLine}";
                }
            }

            return res;
        }
        #endregion
        #endregion [Private]

        #region 加载
        public CsvResult Load()
        {
            if (!File.Exists(_path)) return File_Not_Exist;

            var lines = File.ReadAllLines(_path);

            return Init(lines);
        }

        public async Task<CsvResult> LoadAsync()
        {
            if (!File.Exists(_path)) return File_Not_Exist;

            var lines = await CommonHelper.ReadAllLinesAsync(_path);

            return await InitAsync(lines);
        }
        #endregion

        #region 保存
        public void Save()
        {
            File.WriteAllText(_path, GetSourceString());
        }

        public async Task SaveAsync()
        {
            var str = await Task.Run(() => GetSourceString());
            await CommonHelper.WriteAllTextAsync(_path, str);
        }
        #endregion

        #region 初始化
        public async Task<CsvResult> InitAsync(string[] lines)
        {
            return await Task.Run(() => Init(lines));
        }

        public CsvResult Init(string[] lines)
        {
            Headers.Clear();
            Rows.Clear();

            // 过滤空行：
            var noEmptylines = lines.Where(line => !string.IsNullOrEmpty(line)).ToArray();

            for (int iRow = 0; iRow < noEmptylines.Length; iRow++)
            {
                var line = noEmptylines[iRow];

                // 基类集合：
                var result = GetItemBases(line);
                if (!result.IsSuccess)
                {
                    return new CsvResult(CsvResultType.Error, result.Message);
                }
                if (result.ItemBases == null)
                {
                    var sb = new StringBuilder();
                    sb.AppendLine($"The {nameof(result.ItemBases)} is null!");
                    sb.AppendLine($"Line: {line}");
                    sb.AppendLine($"iRow: {iRow}");
                    return new GetItemBasesResult(null, CsvResultType.Error, sb.ToString());
                }

                if (iRow == 0) // 列头行
                {
                    foreach (var item in result.ItemBases)
                    {
                        Headers.Add(new CsvHeader(this, item));
                    }
                    continue;
                }
                else // 数据行
                {
                    var row = new CsvRow(this);
                    Rows.Add(row);

                    foreach (var item in result.ItemBases)
                    {
                        row.Items.Add(new CsvItem(row, item));
                    }
                }
            }

            return UpdateLength();
        }
        #endregion

        #region 序列化
        public CsvResult Serialization<T>(IList<object> list) where T : class, new()
        {
            var properties = typeof(T).GetProperties();

            Rows.Clear();

            // 属性集合：
            foreach (var obj in list)
            {
                SetObjectToRow(properties, obj);
            }

            return IsVerifyOk();
        }
        #endregion

        #region 反序列化
        public ObservableCollection<object> Deserialization<T>() where T : class, new()
        {
            var properties = typeof(T).GetProperties();
            var list = new ObservableCollection<object>();

            foreach (var item in Rows)
            {
                var obj = new T();

                SetRowToObject(properties, item, obj);

                list.Add(obj);
            }

            return list;
        }
        #endregion

        #region 获取“源数据”字符串
        public string GetSourceString()
        {
            var res = IsVerifyOk();
            if (!res.IsSuccess) return string.Empty;

            var sb = new StringBuilder();

            #region 表头行
            for (int iCol = 0; iCol < Headers.Count; iCol++)
            {
                var header = Headers[iCol];

                sb.Append(header.Source);

                if (iCol < Headers.Count - 1)
                {
                    sb.Append(',');
                }
            }

            sb.AppendLine();
            #endregion 表头行

            #region 数据行
            for (int iRow = 0; iRow < Rows.Count; iRow++)
            {
                var row = Rows[iRow];

                for (int iCol = 0; iCol < Headers.Count; iCol++)
                {
                    var item = row.Items[iCol];

                    sb.Append(item.Source);

                    if (iCol < Headers.Count - 1)
                    {
                        sb.Append(',');
                    }
                }

                if (iRow < Rows.Count - 1)
                {
                    sb.AppendLine();
                }
            }
            #endregion 数据行

            return sb.ToString();
        }
        #endregion

        #region 获取“目标数据”字符串
        public string GetTargetString()
        {
            var sb = new StringBuilder();

            UpdateLength();

            #region 方法：添加“目标数据”
            void AddTarget(int iCol, string target, bool isHeader = false)
            {
                var header = Headers[iCol];

                var maxLength = header.MaxLength;

                if (isHeader)
                {
                    var padding = (maxLength - target.Length) / 2 + _padding;
                    sb.Append(new string(' ', padding));
                    sb.Append(target);
                    sb.Append(new string(' ', padding));

                    if ((maxLength - target.Length) % 2 != 0)
                    {
                        sb.Append(' ');
                    }
                }
                else
                {
                    sb.Append(new string(' ', _padding));
                    sb.Append(target);
                    sb.Append(new string(' ', maxLength - target.Length));
                    sb.Append(new string(' ', _padding));
                }

                if (iCol < Headers.Count - 1)
                {
                    sb.Append('|');
                }
            }
            #endregion

            #region 方法：添加“横线”
            void AddLine()
            {
                sb.Append('+');
                sb.Append(new string('-', RowLength + Headers.Count - 1 + Headers.Count * _padding * 2));
                sb.Append('+');
                sb.AppendLine();
            }
            #endregion

            AddLine();

            #region 添加“表头行”
            sb.Append('|');

            for (int iCol = 0; iCol < Headers.Count; iCol++)
            {
                var header = Headers[iCol];

                AddTarget(iCol, header.Target, true);
            }

            sb.Append('|');
            sb.AppendLine();
            #endregion 添加“表头行”

            AddLine();

            #region 添加“数据行”
            for (int iRow = 0; iRow < Rows.Count; iRow++)
            {
                var row = Rows[iRow];

                sb.Append('|');

                for (int iCol = 0; iCol < Headers.Count; iCol++)
                {
                    var item = row.Items[iCol];

                    AddTarget(iCol, item.Target);
                }

                sb.Append('|');

                if (iRow < Rows.Count - 1)
                {
                    sb.AppendLine();
                }
            }

            if (Rows.Count > 0)
            {
                sb.AppendLine();
            }
            #endregion 添加“数据行”

            AddLine();

            return sb.ToString();
        }
        #endregion

        #region 读取“所有文本”
        public string ReadAllText()
        {
            if (!File.Exists(_path)) return string.Empty;

            return File.ReadAllText(_path);
        }

        public async Task<string> ReadAllTextAsync()
        {
            if (!File.Exists(_path)) return string.Empty;

            return await CommonHelper.ReadAllTextAsync(_path);
        }
        #endregion

        #region 读取“所有行”
        public async Task<string[]> ReadAllLines()
        {
            string[] lines = { };

            if (!File.Exists(_path)) return lines;

            return File.ReadAllLines(_path);
        }

        public async Task<string[]> ReadAllLinesAsync()
        {
            string[] lines = { };

            if (!File.Exists(_path)) return lines;

            await Task.Run(() =>
            {
                lines = File.ReadAllLines(_path);
            });

            return lines;
        }
        #endregion
        #endregion 【Functions】
    }
}
