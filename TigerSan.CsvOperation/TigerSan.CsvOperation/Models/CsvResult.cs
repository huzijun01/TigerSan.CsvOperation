using System.Collections.Generic;

namespace TigerSan.CsvOperation.Models
{
    #region 结果类型
    public enum CsvResultType
    {
        Success,
        Warning,
        Error
    }
    #endregion

    #region 结果
    public class CsvResult
    {
        public bool IsSuccess { get => Equals(Type, CsvResultType.Success); }
        public CsvResultType Type { get; set; } = CsvResultType.Success;
        public string Message { get; set; } = string.Empty;

        #region 【Ctor】
        public CsvResult() { }

        public CsvResult(CsvResultType type, string errorMessage)
        {
            Type = type;
            Message = errorMessage;
        }
        #endregion 【Ctor】
    }
    #endregion

    #region 获取“基类集合”结果
    public class GetItemBasesResult : CsvResult
    {
        public List<CsvItemBase> ItemBases { get; set; }

        #region 【Ctor】
        public GetItemBasesResult(
            List<CsvItemBase> itemBases,
            CsvResultType type = CsvResultType.Success,
            string errorMessage = "") : base(type, errorMessage)
        {
            ItemBases = itemBases != null ? itemBases : new List<CsvItemBase>();
        }
        #endregion 【Ctor】
    }
    #endregion
}
