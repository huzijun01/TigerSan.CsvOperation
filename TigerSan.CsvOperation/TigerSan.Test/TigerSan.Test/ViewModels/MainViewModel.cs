using System.IO;
using System.Windows.Input;
using TigerSan.CsvLog;
using TigerSan.UI.Models;
using TigerSan.UI.Helpers;
using TigerSan.PathOperation;
using TigerSan.CsvOperation.Models;

namespace TigerSan.Test.ViewModels
{
    public class MainViewModel : BindableBase
    {
        #region 【Fields】
        public const string source = @"Files/测试.csv";
        public const string output = @"Files/Output.csv";
        #endregion 【Fields】

        #region 【Properties】
        #region [文本]
        /// <summary>
        /// 源数据
        /// </summary>
        public string Source
        {
            get { return _Source; }
            set { SetProperty(ref _Source, value); }
        }
        private string _Source = string.Empty;

        /// <summary>
        /// “源数据”修改时间
        /// </summary>
        public string SourceTime
        {
            get { return _SourceTime; }
            set { SetProperty(ref _SourceTime, value); }
        }
        private string _SourceTime = string.Empty;

        /// <summary>
        /// 目标数据
        /// </summary>
        public string Target
        {
            get { return _Target; }
            set { SetProperty(ref _Target, value); }
        }
        private string _Target = string.Empty;

        /// <summary>
        /// “目标数据”修改时间
        /// </summary>
        public string TargetTime
        {
            get { return _TargetTime; }
            set { SetProperty(ref _TargetTime, value); }
        }
        private string _TargetTime = string.Empty;

        /// <summary>
        /// 输出数据
        /// </summary>
        public string Output
        {
            get { return _Output; }
            set { SetProperty(ref _Output, value); }
        }
        private string _Output = string.Empty;

        /// <summary>
        /// “输出数据”修改时间
        /// </summary>
        public string OutputTime
        {
            get { return _OutputTime; }
            set { SetProperty(ref _OutputTime, value); }
        }
        private string _OutputTime = string.Empty;
        #endregion [文本]

        #region [列表]
        public TableModel LogTable { get; set; } = new TableModel(typeof(LogData));
        #endregion [列表]
        #endregion 【Properties】

        #region 【Ctor】
        public MainViewModel()
        {
        }
        #endregion 【Ctor】

        #region 【Commands】
        #region 加载完成
        public ICommand LoadedCommand { get => new AsyncDelegateCommand(Loaded); }
        private async Task Loaded()
        {
            //await btnLoad_Click();
        }
        #endregion

        #region [文本]
        #region 点击“加载”按钮
        public ICommand btnLoad_ClickCommand { get => new AsyncDelegateCommand(btnLoad_Click); }
        private async Task btnLoad_Click()
        {
            var model = new CsvOperation.CsvHelper(source);
            var res = await model.LoadAsync();

            if (!res.IsSuccess)
            {
                MsgBox.ShowError(res.Message);
                return;
            }

            Source = model.GetSourceString() ?? "";
            SourceTime = DateTime.Now.ToString();

            Target = model.GetTargetString();
            TargetTime = DateTime.Now.ToString();

            MsgBox.ShowSuccess("加载成功");
        }
        #endregion

        #region 点击“另存”按钮
        public ICommand btnSave_ClickCommand { get => new AsyncDelegateCommand(btnSave_Click); }
        private async Task btnSave_Click()
        {
            var lines = Source.Split(Environment.NewLine);

            var model = new CsvOperation.CsvHelper(output);
            var res = await model.InitAsync(lines);

            if (!res.IsSuccess)
            {
                MsgBox.ShowError(res.Message);
                return;
            }

            await model.SaveAsync();

            Output = await File.ReadAllTextAsync(output);
            OutputTime = DateTime.Now.ToString();

            MsgBox.ShowSuccess("另存成功");
        }
        #endregion

        #region 点击“保存原文本”按钮
        public ICommand btnSaveSource_ClickCommand { get => new AsyncDelegateCommand(btnSaveSource_Click); }
        private async Task btnSaveSource_Click()
        {
            if (!File.Exists(source))
            {
                File.Create(source).Close();
            }

            File.WriteAllText(source, Source);

            MsgBox.ShowSuccess("保存成功");
        }
        #endregion
        #endregion [文本]

        #region [列表]
        #region 点击“刷新”按钮
        public ICommand btnRefresh_ClickCommand { get => new AsyncDelegateCommand(btnRefresh_Click); }
        private async Task btnRefresh_Click()
        {
            var res = await LoadAsync();
            ShowResult(res, "刷新成功");
        }
        #endregion

        #region 点击“添加”按钮
        public ICommand btnAdd_ClickCommand { get => new AsyncDelegateCommand(btnAdd_Click); }
        private async Task btnAdd_Click()
        {
            LogHelper.Instance.Log("Test Log");
            LogHelper.Instance.Warning("Test Warning.");
            LogHelper.Instance.Error("Test Error.");

            var res = await LoadAsync();
            ShowResult(res, "添加成功");
        }
        #endregion

        #region 点击“保存”按钮
        public ICommand btnSaveList_ClickCommand { get => new AsyncDelegateCommand(btnSaveList_Click); }
        private async Task btnSaveList_Click()
        {
            var paths = GetLogPaths();
            if (paths.Length < 1)
            {
                MsgBox.ShowWarning("无日志");
                return;
            }

            var model = new CsvOperation.CsvHelper(paths[0]);
            model.Load();
            model.Serialization<LogData>(LogTable.RowDatas);
            model.Save();

            var res = await LoadAsync();
            ShowResult(res, "保存成功");
        }
        #endregion

        #region 点击“打开目录”按钮
        public ICommand btnOpenDir_ClickCommand { get => new AsyncDelegateCommand(btnOpenDir_Click); }
        private async Task btnOpenDir_Click()
        {
            ExeHelper.OpenPath("Log");
        }
        #endregion

        #region 点击“清空”按钮
        public ICommand btnClear_ClickCommand { get => new AsyncDelegateCommand(btnClear_Click); }
        private async Task btnClear_Click()
        {
            var paths = GetLogPaths();

            foreach (var path in paths)
            {
                File.Delete(path);
            }

            var res = await LoadAsync();
            ShowResult(res, "清空成功", false);
        }
        #endregion
        #endregion [列表]
        #endregion 【Commands】

        #region 【Functions】
        #region 加载
        public async Task<CsvResult> LoadAsync()
        {
            LogTable.RowDatas.Clear();

            var paths = GetLogPaths();
            if (paths.Length < 1)
            {
                return new CsvResult(CsvResultType.Warning, "无日志");
            }

            var model = new CsvOperation.CsvHelper(paths[0]);
            var res = await model.LoadAsync();

            if (!res.IsSuccess)
            {
                return new CsvResult(CsvResultType.Error, res.Message);
            }

            LogTable.RowDatas = model.Deserialization<LogData>();

            return new CsvResult();
        }
        #endregion

        #region 获取Log路径集合
        public static string[] GetLogPaths(string baseDirectory = "./Log")
        {
            // 检查文件夹是否存在
            if (!Directory.Exists(baseDirectory))
            {
                LogHelper.Instance.Warning($"目录不存在: {baseDirectory}");
                return [];
            }

            // 获取所有csv文件并筛选符合条件的文件
            var files = Directory.GetFiles(baseDirectory, "*.csv")
                .Where(file =>
                    Path.GetFileName(file)
                    .StartsWith("log_", StringComparison.OrdinalIgnoreCase))
                .ToArray();

            return files;
        }
        #endregion

        #region 显示结果
        public void ShowResult(
            CsvResult res,
            string strSuccee,
            bool isShowWarning = true)
        {
            switch (res.Type)
            {
                case CsvResultType.Warning:
                    if (isShowWarning)
                    {
                        MsgBox.ShowWarning(res.Message);
                    }
                    break;
                case CsvResultType.Error:
                    MsgBox.ShowError(res.Message);
                    break;
                case CsvResultType.Success:
                    MsgBox.ShowSuccess(strSuccee);
                    break;
            }
        }
        #endregion
        #endregion 【Functions】
    }

    #region 设计数据
    public class DesignMainViewModel : MainViewModel
    {
        public DesignMainViewModel()
        {
        }
    }
    #endregion
}
