using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Win32;
using Path = System.IO.Path;

namespace CutMovies
{
    public sealed class DataStore
    {
        static DataStore()
        {
        }

        private DataStore()
        {
        }

        public static DataStore Instance { get; } = new DataStore();
    }
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private string FfmpegPath { get; set; } = @"C:\root\shortcut\ffmpeg.exe";
        public MainWindow()
        {
            InitializeComponent();
            var curDirrectory = System.IO.Directory.GetCurrentDirectory();
            var inputPath = curDirrectory + @"\input";
            this.TextBlockFileName.Text = inputPath;
            this.TextBlockFileName.ToolTip = inputPath;
            this.FfmpegPath = curDirrectory + @"\ffmpeg.exe";
            ToggleProgressBarVisibility();
        }
        /// <summary>
        /// 分離ボタンイベント実行
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void BtnSeparation_Click(object sender, RoutedEventArgs e)
        {
            ToggleProgressBarVisibility();
            var contexts = CreateMovieContexts();
            await Task.Run(() => ExecuteSeparation(contexts));
            ShowCompleteDialog();
            this.BtnJoin.IsEnabled = true;
        }
        /// <summary>
        /// プログレスバーの表示切替
        /// </summary>
        private void ToggleProgressBarVisibility()
        {
            this.ProgressBar.Visibility = this.ProgressBar.Visibility == Visibility.Visible ? Visibility.Hidden : Visibility.Visible;
        }
        /// <summary>
        /// 分離処理
        /// </summary>
        /// <param name="contexts"></param>
        private void ExecuteSeparation(List<GetMovieContext> contexts)
        {
            var info = GetAllMoviePartsList(contexts);

            CreatePartMovie(info);
        }
        /// <summary>
        /// 処理完了ダイアログ表示
        /// </summary>
        private void ShowCompleteDialog()
        {
            ToggleProgressBarVisibility();
            this.Activate();
            Dialog dialog = new Dialog();
            dialog.Owner = this;
            dialog.Show();
        }

        /// <summary>
        /// 元になる動画データの情報を取得
        /// </summary>
        /// <returns></returns>
        private List<GetMovieContext> CreateMovieContexts()
        {
            var curDirrectory = System.IO.Directory.GetCurrentDirectory();
            var inputDirectory = this.TextBlockFileName.Text;
            string[] files = System.IO.Directory.GetFiles(
                inputDirectory, "*", System.IO.SearchOption.TopDirectoryOnly);
            var outputDir = inputDirectory + @"\output\";

            var tmpIntervalTime = this.IntervalTime.Text;
            var parseRet = double.TryParse(tmpIntervalTime, out double intervalTime);
            if (parseRet)
            {
                intervalTime = 1 / intervalTime;
            }
            else
            {
                intervalTime = 1;
            }

            List<GetMovieContext> contexts = new List<GetMovieContext>();
            foreach (var file in files)
            {
                var context = 
                    new GetMovieContext(file, 
                    outputDir, 
                    0, 
                    0,
                    this.TxtCreateTimeCondition.Text,
                    intervalTime,
                    this.TxtNoSoundLevel.Text,
                    this.TxtNoSoundTerm.Text);
                contexts.Add(context);
            }
            return contexts;
        }
        /// <summary>
        /// 有音区間の動画を生成
        /// </summary>
        /// <param name="mInfoList"></param>
        private void CreatePartMovie(List<MovieInfomation> mInfoList)
        {
            foreach (var mInfo in mInfoList)
            {
                var outPutFileList = CreatePartMovies(mInfo);
                CreateSummaryTxt(outPutFileList, mInfo.Context.OutputDirectoryPath);

            }
        }
        /// <summary>
        /// それぞれの有音区間の動画を作成し、出力データのリストを返す
        /// </summary>
        /// <param name="mInfo"></param>
        /// <returns></returns>
        private List<string> CreatePartMovies(MovieInfomation mInfo)
        {
            var context = mInfo.Context;
            var partsData = mInfo.PartsData;
            // 出力先のフォルダ
            var outPutPath = context.OutputDirectoryPath;
            // 出力先のフォルダを作成
            Directory.CreateDirectory(outPutPath);
            // 
            List<string> outPutFileList = new List<string>();
            foreach (var part in partsData)
            {
                if (!part.IsCreate)
                {
                    continue;
                }

                var oFileName = $@"output{part.Number}.mp4";
                var fullFileName = outPutPath+ @"\" + oFileName;
                outPutFileList.Add(fullFileName);
                var arguments =
                    $@"-ss {part.From + context.StartDelayDuration} -i {context.InputMoviePath} -t {part.Duration + context.StartDelayDuration + context.EndDelayDuration}  {fullFileName}";
                FfmpegExecute(arguments);
            }

            return outPutFileList;
        }
        /// <summary>
        /// 結合の際に必要となるtxtデータを生成
        /// </summary>
        /// <param name="outPutFileList"></param>
        /// <param name="outPutPath"></param>
        private static void CreateSummaryTxt(List<string> outPutFileList, string outPutPath)
        {
            var summaryPath = GetOutPutFileSummaryPath(outPutPath);
            using StreamWriter writer = new StreamWriter(summaryPath, false);
            foreach (var file in outPutFileList)
            {
                writer.WriteLine(@"file " + file.Replace(@"\", @"\\"));
            }
        }

        /// <summary>
        /// 出力済みパート動画の情報をテキストで出力しておく。後に結合時に必要
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        private static string GetOutPutFileSummaryPath(string path)
        {
            var outPutFilesSummary = path + @"\summary.txt";
            return outPutFilesSummary;
        }
        public List<MovieInfomation> GetAllMoviePartsList(List<GetMovieContext> movieContexts)
        {
            List<MovieInfomation> movieInfomations = new List<MovieInfomation>();
            foreach (var context in movieContexts)
            {
                var parts = CreatePartList(context);
                var movie = new MovieInfomation(context, parts);
                movieInfomations.Add(movie);

            }

            return movieInfomations;
        }
        /// <summary>
        /// 動画から有音区間の情報を作成
        /// </summary>
        /// <param name="getMovieContext"></param>
        /// <returns></returns>
        private List<SoundPartInfo> CreatePartList(GetMovieContext getMovieContext)
        {
            var arguments = $@"-i {getMovieContext.InputMoviePath} -af silencedetect=noise={getMovieContext.NoSoundLevel}dB:d={getMovieContext.NoSoundTerm} -f null -";
            var rawinfo = FfmpegExecute(arguments);

            var tmpInfo = rawinfo.Replace(Environment.NewLine, " ").Split(' ');

            List<SoundPartInfo> soundPartInfoList = new List<SoundPartInfo>();
            List<double> startEndTimeList = new List<double>();
            for (int i = 0; i < tmpInfo.Length; i++)
            {
                if (tmpInfo[i] == "silence_start:" || tmpInfo[i] == "silence_end:")
                {
                    startEndTimeList.Add(double.Parse(tmpInfo[i + 1]));
                }
            }

            int num = 0;
            for (int i = 0; i < startEndTimeList.Count; i += 2)
            {
                if (i == 0)
                {
                    continue;
                }

                soundPartInfoList.Add(new SoundPartInfo(num++, startEndTimeList[i - 1], startEndTimeList[i],getMovieContext.CreateTimeCondition));
            }

            return soundPartInfoList;
        }
        /// <summary>
        /// 結合ボタン実行
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void BtnJoin_Click(object sender, RoutedEventArgs e)
        {
            ToggleProgressBarVisibility();
            var contexts = CreateMovieContexts();
            await Task.Run(() => ExecuteJoinPartMovies(contexts));
            ShowCompleteDialog();
        }
        /// <summary>
        /// 結合処理
        /// </summary>
        /// <param name="getMovieContexts"></param>
        private void ExecuteJoinPartMovies(List<GetMovieContext> getMovieContexts)
        {
            var curDirrectory = System.IO.Directory.GetCurrentDirectory();
            var outputDir = curDirrectory + @"\output\";
            foreach (var context in getMovieContexts)
            {
                var summaryPath = GetOutPutFileSummaryPath(context.OutputDirectoryPath);

                //var arguments = $@"-f concat -i {summaryPath} -c copy {outputDir}Summary.mp4";
                var arguments =
                    $@"-safe 0 -f concat -i {summaryPath} -c:v copy -c:a copy -map 0:v -map 0:a {context.OutputDirectoryPath}\complete.mp4";
                FfmpegExecute(arguments);
            }
        }

        private async void ButtonThumbnail_Click(object sender, RoutedEventArgs e)
        {
            ToggleProgressBarVisibility();
            var contexts = CreateMovieContexts();
            await Task.Run(() => ExecuteMakeThumbnail(contexts));
            ShowCompleteDialog();
        }
        /// <summary>
        /// サムネイル作成処理実行
        /// </summary>
        /// <param name="contexts"></param>
        private void ExecuteMakeThumbnail(List<GetMovieContext> contexts)
        {
            
            foreach (var context in contexts)
            {
                // 出力先のフォルダ
                var outPutPath =
                    $@"{context.OutputDirectoryPath}\thumbnail";
                // 出力先のフォルダを作成
                Directory.CreateDirectory(outPutPath);

                var arguments =
                    $@"-i {context.InputMoviePath} -filter:v fps=fps={context.IntervalTime}:round=down -q:v 0.2 {outPutPath}\%09d.jpg";

                FfmpegExecute(arguments);
            }
        }

        /// <summary>
        /// ffmpegのプロセスをスタート
        /// </summary>
        /// <param name="arguments"></param>
        /// <returns></returns>
        private string FfmpegExecute(string arguments)
        {
            using var process = new Process();
            process.StartInfo = new ProcessStartInfo
            {
                FileName = FfmpegPath,
                Arguments = arguments,
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            process.Start();
            var tmp = process.StandardError.ReadToEnd();
            process.WaitForExit();
            if (process.HasExited)
            {
                process.Kill();
            }

            return tmp;
        }
        /// <summary>
        /// 全実行ボタンイベント
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private async void BtnAllExecute_Click(object sender, RoutedEventArgs e)
        {
            ToggleProgressBarVisibility();
            var contexts = CreateMovieContexts();
            await Task.Run(() => ExecuteSeparation(contexts));
            await Task.Run(() => ExecuteMakeThumbnail(contexts));
            await Task.Run(() => ExecuteJoinPartMovies(contexts));
            ShowCompleteDialog();
        }
        /// <summary>
        /// パス設定のファイルダイアログを表示
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FileOpenButton_Click(object sender, RoutedEventArgs e)
        {
            string initialFileName = "SelectFolder Or Files";
            var dialog = new OpenFileDialog()
            {
                FileName = initialFileName, 
                CheckFileExists = false,
                CheckPathExists = true,
                Title = "ファイルを開く",
                Filter = "全てのファイル(*.*)|*a*",
                Multiselect = true
            };
            if (dialog.ShowDialog() == true)
            {
                if (dialog.SafeFileName == initialFileName)
                {
                    this.TextBlockFileName.Text = Path.GetDirectoryName(dialog.FileName);
                    this.TextBlockFileName.ToolTip = Path.GetDirectoryName(dialog.FileName);
                    return;
                }
                this.TextBlockFileName.Text = string.Join(Environment.NewLine, dialog.SafeFileNames);
                this.TextBlockFileName.ToolTip = string.Join(Environment.NewLine, dialog.SafeFileNames);
            }
            else
            {
                this.TextBlockFileName.Text = "キャンセルされました";
            }
        }
    }
}
