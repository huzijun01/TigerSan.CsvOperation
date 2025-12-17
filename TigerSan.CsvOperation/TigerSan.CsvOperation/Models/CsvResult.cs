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

        public CsvResult(CsvResultType type, string message)
        {
            Type = type;
            Message = message;
        }

        public CsvResult(GetOneRowItemBasesResult resItemBases)
        {
            Type = resItemBases.Type;
            Message = resItemBases.Message;
        }
        #endregion 【Ctor】

        #region 【Functions】
        #region 获取“错误”结果
        public static CsvResult Error(string msg)
        {
            return new CsvResult(CsvResultType.Error, msg);
        }
        #endregion
        #endregion 【Functions】
    }
    #endregion

    #region 获取“行项目基类”方法结果
    public class GetItemBaseResult : CsvResult
    {
        public CsvItemBase ItemBase { get; set; } = new CsvItemBase();

        #region 【Ctor】
        public GetItemBaseResult() { }

        public GetItemBaseResult(
            CsvItemBase item,
            CsvResult csvResult)
        {
            ItemBase = item;
            Type = csvResult.Type;
            Message = csvResult.Message;
        }

        public GetItemBaseResult(
            CsvItemBase item,
            CsvResultType type = CsvResultType.Success,
            string message = "")
        {
            ItemBase = item;
            Type = type;
            Message = message;
        }
        #endregion 【Ctor】
    }
    #endregion

    #region 获取“行项目基类集合”方法结果
    public class GetOneRowItemBasesResult : CsvResult
    {
        public List<CsvItemBase> ItemBases { get; set; } = new List<CsvItemBase>();

        #region 【Ctor】
        public GetOneRowItemBasesResult() { }

        public GetOneRowItemBasesResult(GetItemBaseResult resItemBase)
        {
            Type = resItemBase.Type;
            Message = resItemBase.Message;
        }

        public GetOneRowItemBasesResult(
            List<CsvItemBase> items,
            CsvResult csvResult)
        {
            ItemBases = items;
            Type = csvResult.Type;
            Message = csvResult.Message;
        }

        public GetOneRowItemBasesResult(
            List<CsvItemBase> items,
            CsvResultType type = CsvResultType.Success,
            string message = "")
        {
            ItemBases = items;
            Type = type;
            Message = message;
        }
        #endregion 【Ctor】
    }
    #endregion
}
