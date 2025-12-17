using System.Text;
using System.Collections.Generic;
using TigerSan.CsvOperation.Helpers;

namespace TigerSan.CsvOperation.Models
{
    #region 项目基类
    /// <summary>
    /// 项目基类
    /// </summary>
    public class CsvItemBase
    {
        #region 【Fields】
        /// <summary>
        /// “源数据”构建器
        /// </summary>
        private readonly StringBuilder _sbSource = new StringBuilder();

        /// <summary>
        /// “目标数据”构建器
        /// </summary>
        private readonly StringBuilder _sbTarget = new StringBuilder();
        #endregion 【Fields】

        #region 【Properties】
        /// <summary>
        /// 源数据
        /// （同步修改“目标数据”）
        /// </summary>
        public string Source
        {
            get => _sbSource.ToString();
            set => SetSource(value);
        }

        /// <summary>
        /// 目标数据
        /// （同步修改“源数据”）
        /// </summary>
        public string Target
        {
            get => _sbTarget.ToString();
            set => SetTarget(value);
        }
        #endregion 【Properties】

        #region 【Ctor】
        public CsvItemBase()
        {
        }

        public CsvItemBase(CsvItemBase itemBase)
        {
            _sbSource = new StringBuilder(itemBase.Source);
            _sbTarget = new StringBuilder(itemBase.Target);
        }
        #endregion 【Ctor】

        #region 【Functions】
        #region 从“目标数据”获取“源数据”
        public string GetSourceFromTarget()
        {
            return GetSourceFromTarget(Target);
        }

        public string GetSourceFromTarget(string target)
        {
            var str = target.Replace("\"", "\"\"");

            if (target.Contains("\"") || target.Contains(",") || target.Contains("\n"))
            {
                str = $"\"{str}\"";
            }

            return str;
        }
        #endregion

        #region 从“源数据”获取“目标数据”
        public string GetTargetFromSource()
        {
            return GetTargetFromSource(Source);
        }

        public string GetTargetFromSource(string source)
        {
            var str = source;

            if (source.Contains("\""))
            {
                str = CommonHelper.RemoveFirstAndLastChar(str);
            }

            str = str.Replace("\"\"", "\"");

            return str;
        }
        #endregion

        #region 设置“源数据”
        public CsvResult SetSource(string value)
        {
            var res = CommonHelper.IsSourceVerifyOk(value);
            if (!res.IsSuccess) return res;

            _sbSource.Clear();
            _sbSource.Append(value);
            _sbTarget.Clear();
            _sbTarget.Append(GetTargetFromSource(value));

            return res;
        }
        #endregion

        #region 设置“目标数据”
        public void SetTarget(string value)
        {
            _sbTarget.Clear();
            _sbTarget.Append(value);
            _sbSource.Clear();
            _sbSource.Append(GetSourceFromTarget(value));
        }
        #endregion
        #endregion 【Functions】
    }
    #endregion

    #region 列头
    /// <summary>
    /// 列头
    /// </summary>
    public class CsvHeader : CsvItemBase
    {
        #region 【Fields】
        /// <summary>
        /// 模型
        /// </summary>
        public readonly CsvHelper _model;
        #endregion 【Fields】

        #region 【Properties】
        /// <summary>
        /// 最大长度
        /// </summary>
        public int MaxLength { get; set; }
        #endregion 【Properties】

        #region 【Ctor】
        public CsvHeader(CsvHelper model)
        {
            _model = model;
        }

        public CsvHeader(CsvHelper model, CsvItemBase itemBase) : base(itemBase)
        {
            _model = model;
        }
        #endregion 【Ctor】
    }
    #endregion

    #region 项目
    /// <summary>
    /// 项目
    /// </summary>
    public class CsvItem : CsvItemBase
    {
        #region 【Fields】
        /// <summary>
        /// 行
        /// </summary>
        public readonly CsvRow _row;
        #endregion 【Fields】

        #region 【Ctor】
        public CsvItem(CsvRow row)
        {
            _row = row;
        }

        public CsvItem(CsvRow row, CsvItemBase itemBase) : base(itemBase)
        {
            _row = row;
        }
        #endregion 【Ctor】
    }
    #endregion

    #region 行
    /// <summary>
    /// 行
    /// </summary>
    public class CsvRow
    {
        #region 【Fields】
        /// <summary>
        /// 模型
        /// </summary>
        public readonly CsvHelper _model;
        #endregion 【Fields】

        /// <summary>
        /// 项目集合
        /// </summary>
        public List<CsvItem> Items { get; set; } = new List<CsvItem>();

        #region 【Ctor】
        public CsvRow(CsvHelper model)
        {
            _model = model;
        }
        #endregion 【Ctor】
    }
    #endregion
}
