using Microsoft.Win32;
using PropertyChanged;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using WpfTypingPractice.Helpers;

namespace WpfTypingPractice.ViewModels
{
    public class MainWindowViewModel : BaseViewModel
    {
        public MainWindowViewModel()
        {
            MainGridMouseClickCommand = new RelayCommand(OpenTxtFile);
            WindowKeyOpenFileCommand = new RelayCommand(OpenTxtFile);
            WindowEscCommand = new RelayCommand(() =>
            {
                Input = "";
            });
            InputModeButtonCommand = new RelayCommand(() =>
            {
                IsUsingShuangpin = !IsUsingShuangpin;
            });

            Article = Finished = "双击上方任意位置打开外部文本";
        }

        #region 与打字相关

        private int currentIndex = 0;

        /// <summary>
        /// 开始打字前的准备工作
        /// </summary>
        public void GetReady()
        {
            Finished = "";
            currentIndex = 0;
            TypedWordCount = 0;
            SkipInvalidChars();
        }

        /// <summary>
        /// 跳过之后的所有无需练习的内容
        /// </summary>
        private void SkipInvalidChars()
        {
            for (int i = currentIndex; i < Article.Length; i++)
            {
                if (StringHelper.IsValidChineseChar(Article[i]))
                    break;
                Finished += Article[currentIndex++];
            }
        }

        private bool IsFinished
        {
            get => Article == Finished;
        }

        public bool CanInput
        {
            get => !IsFinished;
        }

        private string input;
        /// <summary>
        /// 用户输入框，控制打字的逻辑
        /// </summary>
        public string Input
        {
            get { return input; }
            set
            {
                // 如果已经完成，则不响应
                if (IsFinished)
                {
                    input = "";
                    return;
                }

                // 如果输入的是空白，那么会清空
                if (string.IsNullOrWhiteSpace(value))
                {
                    input = "";
                    return;
                }

                char cur = Article[currentIndex];
                string[] pinyins = StringHelper.GetPinyins(cur);
                if (IsUsingShuangpin)
                    pinyins = pinyins.Select(p => StringHelper.GetShuangpin(p)).ToArray();
                // 如果输入的是正确的拼音
                if (value.ToLower().EqualsAny(pinyins))
                {
                    Finished += cur;
                    currentIndex++;
                    TypedWordCount++;
                    SkipInvalidChars();
                    input = "";
                    CalculateSpeed();
                    return;
                }
                // 如果输入的拼音长度超过最长的可能拼音
                if (value.ToLower().Length > pinyins.Max(p => p.Length))
                {
                    input = "";
                    return;
                }
                input = value;
            }
        }

        /// <summary>
        /// 是否使用双拼
        /// </summary>
        [AlsoNotifyFor("InputMode")]
        public bool IsUsingShuangpin { get; set; } = false;

        #endregion

        #region 速度和字数统计

        /// <summary>
        /// 统计每次打字的时刻
        /// </summary>
        private LinkedList<DateTime> keyPressTicks = new LinkedList<DateTime>();
        /// <summary>
        /// 最大的时间间隔
        /// </summary>
        private TimeSpan maxTimeSpan = new TimeSpan(0, 0, 10);
        /// <summary>
        /// 最长的用来计算速度的时刻数量
        /// </summary>
        private int maxLength = 30;

        /// <summary>
        /// 打字速度
        /// </summary>
        public int TypingSpeed { get; set; } = 0;
        /// <summary>
        /// 已经打过的字数统计
        /// </summary>
        public int TypedWordCount { get; set; } = 0;

        private void CalculateSpeed()
        {
            var cur = DateTime.Now;
            // 如果时间列表是空的
            if (keyPressTicks.Count == 0)
            {
                keyPressTicks.AddLast(cur);
                TypingSpeed = 0;
                return;
            }
            var last = keyPressTicks.Last.Value;
            // 如果距离上次的时间太久，则前面的全部作废
            if (cur - last > maxTimeSpan)
            {
                keyPressTicks.Clear();
                keyPressTicks.AddLast(cur);
                TypingSpeed = 0;
                return;
            }

            keyPressTicks.AddLast(cur);
            // 如果储存过多，则删掉前面的
            while (keyPressTicks.Count > maxLength)
            {
                keyPressTicks.RemoveFirst();
            }
            // 如果元素超过一个，则计算速度
            if (keyPressTicks.Count > 1)
            {
                var timespan = (keyPressTicks.Last.Value - keyPressTicks.First.Value).TotalSeconds;
                TypingSpeed = (int)((keyPressTicks.Count - 1) / timespan * 60);
                return;
            }

            TypingSpeed = 0;
        }

        #endregion

        #region 界面显示的绑定

        public string ArticleTitle { get; set; }

        public Visibility IsTitleVisible
        {
            get
            {
                if (string.IsNullOrEmpty(ArticleTitle))
                    return Visibility.Collapsed;
                else
                    return Visibility.Visible;
            }
        }

        /// <summary>
        /// 用来练习打字的文本
        /// </summary>
        public string Article { get; set; }

        /// <summary>
        /// 已经打过的内容
        /// </summary>
        public string Finished { get; set; }

        /// <summary>
        /// 当前的输入方式
        /// </summary>
        public string InputMode
        {
            get
            {
                if (IsUsingShuangpin)
                    return "双拼";
                else
                    return "全拼";
            }
        }

        #endregion

        #region 元件绑定的 Command

        /// <summary>
        /// Ctrl+O 打开外部文件
        /// </summary>
        public ICommand WindowKeyOpenFileCommand { get; set; }

        /// <summary>
        /// 双击屏幕上方文字部分，打开外部文件
        /// </summary>
        public ICommand MainGridMouseClickCommand { get; set; }

        /// <summary>
        /// 按下 ESC，清空输入框
        /// </summary>
        public ICommand WindowEscCommand { get; set; }

        /// <summary>
        /// 切换输入方式的按钮
        /// </summary>
        public ICommand InputModeButtonCommand { get; set; }

        #endregion

        #region 其他方法

        /// <summary>
        /// 打开外部文本文件
        /// </summary>
        public void OpenTxtFile()
        {
            OpenFileDialog open = new OpenFileDialog
            {
                Filter = "文本文件|*.txt"
            };
            if (open.ShowDialog().Value)
            {
                var filename = open.FileName;
                // 检测文本文件的编码
                var encoding = StringHelper.GetEncoding(filename);

                // 无法获取编码，放弃读取
                if (encoding is null)
                {
                    MessageBox.Show("提示", "无法识别文本文件的文本编码。");
                    return;
                }

                // 开始读取
                using (var reader = new StreamReader(filename, encoding))
                {
                    var article = reader.ReadToEnd().Trim();
                    Article = StringHelper.AutoInsertEmptyLine(article);

                    // 读取的文本文件内容是空的（不具有用来练习的文本内容）
                    if (string.IsNullOrWhiteSpace(Article))
                    {
                        MessageBox.Show("提示", "外部文本内容是空的。请选择其他文本文件。");
                        return;
                    }

                    ArticleTitle = Path.GetFileNameWithoutExtension(filename);
                    if (ArticleTitle.Length > 30)
                        ArticleTitle = ArticleTitle.Substring(0, 30) + "……";

                    GetReady();
                }
            }
        }

        #endregion
    }
}
