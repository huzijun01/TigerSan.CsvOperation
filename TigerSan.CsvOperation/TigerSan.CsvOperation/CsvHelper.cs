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
        #region [Private]
        /// <summary>
        /// 当前行号
        /// </summary>
        private int _iRow = 0;

        /// <summary>
        /// 当前列号
        /// </summary>
        private int _iCol = 0;

        /// <summary>
        /// 所有行
        /// </summary>
        private string[] _lines = { };
        #endregion [Private]

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
        #region [Private]
        #region 报错
        /// <summary>
        /// 文件不存在！
        /// </summary>
        private CsvResult File_Not_Exist { get => CsvResult.Error($"The file does not exist!{Environment.NewLine}{_path}"); }
        /// <summary>
        /// 这里不应该出现双引号！
        /// </summary>
        private CsvResult Abnormal_Double_Quotation { get => Error($"Double quotation should not appear here!"); }
        /// <summary>
        /// 双引号未闭合！
        /// </summary>
        private CsvResult Double_Quotation_Not_Closed { get => Error($"The Double quotation are not closed!"); }
        /// <summary>
        /// 内容不能包含单个双引号！
        /// </summary>
        private CsvResult ContainSingleDoubleQuotation { get => Error($"The content should not contain single double quotation marks!"); }
        /// <summary>
        /// 此处不应出现逗号！
        /// </summary>
        private CsvResult Abnormal_Comma { get => Error($"Comma should not appear here!"); }
        /// <summary>
        /// 此处不应出现换行符！
        /// </summary>
        private CsvResult Abnormal_LineBreak { get => Error($"Line break should not appear here!"); }
        #endregion 报错

        #region 判断
        /// <summary>
        /// 是否到达行尾
        /// </summary>
        private bool Is_End_Of_Line { get => _iCol >= Chars.Length - 1; }

        /// <summary>
        /// 是否有下一个字符
        /// </summary>
        private bool Is_Have_Following_Char { get => _iCol < Chars.Length - 1; }

        /// <summary>
        /// 列索引是否在范围内
        /// </summary>
        private bool Is_Col_Index_Within_The_Range { get => _iCol < Chars.Length; }

        /// <summary>
        /// 行索引是否在范围内
        /// </summary>
        private bool Is_Row_Index_Within_The_Range { get => _iRow < _lines.Length; }

        /// <summary>
        /// 当前字符是否为逗号
        /// </summary>
        private bool Current_Char_Is_Comma { get => Is_Col_Index_Within_The_Range && Equals(Chars[_iCol], ','); }

        /// <summary>
        /// 当前字符是否为换行符
        /// </summary>
        private bool Current_Char_Is_LineBreak { get => Is_Col_Index_Within_The_Range && Equals(Chars[_iCol], '\n'); }

        /// <summary>
        /// 当前字符是否为双引号
        /// </summary>
        private bool Current_Char_Is_Double_Quotation { get => Is_Col_Index_Within_The_Range && Equals(Chars[_iCol], '"'); }

        /// <summary>
        /// 下一个字符是否为逗号
        /// </summary>
        private bool Following_Char_Is_Comma { get => Is_Have_Following_Char && Equals(Chars[_iCol + 1], ','); }

        /// <summary>
        /// 下一个字符是否为双引号
        /// </summary>
        private bool Following_Char_Is_Double_Quotation { get => Is_Have_Following_Char && Equals(Chars[_iCol + 1], '"'); }
        #endregion 判断

        /// <summary>
        /// 当前行内容
        /// </summary>
        private string Line { get => Is_Row_Index_Within_The_Range ? _lines[_iRow] : ""; }

        /// <summary>
        /// 当前字符数组
        /// </summary>
        private char[] Chars { get => Is_Row_Index_Within_The_Range ? _lines[_iRow].ToCharArray() : new List<char>().ToArray(); }
        #endregion [Private]

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
        #region 获取“错误”结果
        private CsvResult Error(string msg)
        {
            var sb = new StringBuilder();
            sb.AppendLine("CSV Format Error:");
            sb.AppendLine(msg);
            sb.AppendLine($"{nameof(_iRow)} = {_iRow}");
            sb.AppendLine($"{nameof(_iCol)} = {_iCol}");
            sb.AppendLine($"{nameof(Line)} = {Line}");
            return CsvResult.Error(sb.ToString());
        }
        #endregion

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

        #region 获取“普通基类项目”
        private GetItemBaseResult GetNormalItemBase()
        {
            var sbItem = new StringBuilder();

            while (_iCol < Chars.Length)
            {
                var ch = Line[_iCol];

                #region 结束
                if (Current_Char_Is_Comma) // “逗号”结尾
                {
                    ++_iCol; // 跳过结束逗号
                    break;
                }
                else if (Is_End_Of_Line) // 到达行尾
                {
                    sbItem.Append(ch);
                    break;
                }
                #endregion 结束

                #region 异常
                if (Current_Char_Is_Double_Quotation)
                {
                    return new GetItemBaseResult(null, Abnormal_Double_Quotation);
                }
                else if (Current_Char_Is_LineBreak)
                {
                    return new GetItemBaseResult(null, Abnormal_LineBreak);
                }
                #endregion 异常

                sbItem.Append(ch);

                ++_iCol;
            }

            return new GetItemBaseResult(new CsvItemBase() { Source = sbItem.ToString() });
        }
        #endregion

        #region 获取“特殊基类项目”
        private GetItemBaseResult GetSpecialItemBase()
        {
            bool isEndCopy = false;
            var sbItem = new StringBuilder();
            int quotCount = 0;

            sbItem.Append(Line[_iCol]);
            ++_iCol; // 跳过起始引号
            ++quotCount;

            while (_iRow < _lines.Length && !isEndCopy)
            {

                while (_iCol < Chars.Length)
                {
                    var ch = Line[_iCol];

                    #region 跳过
                    if (Current_Char_Is_Double_Quotation && Following_Char_Is_Double_Quotation) // 一对“引号”
                    {
                        sbItem.Append(ch);
                        sbItem.Append(Line[_iCol + 1]);
                        _iCol += 2; // 跳过“一对引号”中的“第二个引号”
                        quotCount += 2;
                        continue;
                    }
                    #endregion 跳过

                    #region 结束
                    if (Current_Char_Is_Double_Quotation && Following_Char_Is_Comma) // “引号+逗号”结尾
                    {
                        sbItem.Append(ch);
                        _iCol += 2; // 跳过“引号+逗号”
                        isEndCopy = true;
                        break;
                    }
                    else if (Is_End_Of_Line
                        && Current_Char_Is_Double_Quotation
                        && quotCount % 2 != 0) // 到达行尾，且“引号”即将闭合
                    {
                        sbItem.Append(ch);
                        isEndCopy = true;
                        break;
                    }
                    #endregion 结束

                    #region 异常
                    if (Current_Char_Is_Double_Quotation && !Following_Char_Is_Double_Quotation) // 单个“引号”
                    {
                        return new GetItemBaseResult(null, ContainSingleDoubleQuotation);
                    }
                    #endregion 异常

                    sbItem.Append(ch);

                    ++_iCol;
                }

                if (isEndCopy)
                {
                    break;
                }
                else
                {
                    ++_iRow;
                    _iCol = 0;
                    sbItem.AppendLine();
                }
            }

            return new GetItemBaseResult(new CsvItemBase() { Source = sbItem.ToString() });
        }
        #endregion

        #region 获取“一行数据”的“基类项目”集合
        private GetOneRowItemBasesResult GetOneRowItemBases()
        {
            var itemBases = new List<CsvItemBase>();

            while (!Is_End_Of_Line)
            {
                var ch = Chars[_iCol];
                var res = new GetItemBaseResult();

                if (Current_Char_Is_Double_Quotation) // 引号开头
                {
                    res = GetSpecialItemBase();
                }
                else // 普通字符开头
                {
                    res = GetNormalItemBase();
                }

                if (!res.IsSuccess) return new GetOneRowItemBasesResult(res);

                itemBases.Add(res.ItemBase);

                if (Is_End_Of_Line)
                {
                    _iCol = 0;
                    ++_iRow;
                    break;
                }
            }
            return new GetOneRowItemBasesResult(itemBases);
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
        public CsvResult Init(string[] lines)
        {
            _lines = lines;

            #region 清空
            _iRow = 0;
            _iCol = 0;
            Headers.Clear();
            Rows.Clear();
            #endregion 清空

            #region 判错
            if (_lines.Length < 1)
            {
                return new CsvResult(CsvResultType.Error, "The CSV data is empty!");
            }
            #endregion 判错

            #region 列头行
            // 获取“基类项目”集合：
            var resHeaderItemBases = GetOneRowItemBases();
            if (!resHeaderItemBases.IsSuccess) return new CsvResult(resHeaderItemBases);

            // 添加“表头项目”：
            foreach (var itemBase in resHeaderItemBases.ItemBases)
            {
                var header = new CsvHeader(this) { Source = itemBase.Source };
                Headers.Add(header);
            }
            #endregion 列头行

            #region 数据行
            while (_iRow < _lines.Length)
            {
                var row = new CsvRow(this);
                Rows.Add(row);

                // 获取“基类项目”集合：
                var resDataItemBases = GetOneRowItemBases();
                if (!resDataItemBases.IsSuccess) return new CsvResult(resDataItemBases);

                // 添加“数据项目”：
                foreach (var itemBase in resDataItemBases.ItemBases)
                {
                    var item = new CsvItem(row) { Source = itemBase.Source };
                    row.Items.Add(item);
                }
            }
            #endregion 数据行

            return UpdateLength();
        }

        #region 初始化（异步）
        public async Task<CsvResult> InitAsync(string[] lines)
        {
            return await Task.Run(() => Init(lines));
        }
        #endregion
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
                target = CommonHelper.GetNoNewLineString(target); // 去除换行符

                var header = Headers[iCol];

                var maxLength = header.MaxLength;

                if (isHeader) // 列头居中
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
                else // 数据左对齐
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
