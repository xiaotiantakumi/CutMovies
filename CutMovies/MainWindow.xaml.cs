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
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void BtnSeparation_Click(object sender, RoutedEventArgs e)
        {
            ExecuteSeparation();
            ShowCompleteDialog();
        }

        private void ExecuteSeparation()
        {
            var contexts = CreateMovieContexts();
            var info = GetAllMoviePartsList(contexts);

            CreatePartMovie(info);
        }

        private void ShowCompleteDialog()
        {
            Dialog dialog = new Dialog();
            dialog.Owner = this;
            dialog.Show();
        }

        /// <summary>
        /// 元になる動画データの情報を取得
        /// </summary>
        /// <returns></returns>
        private static List<GetMovieContext> CreateMovieContexts()
        {
            var curDirrectory = System.IO.Directory.GetCurrentDirectory();
            var inputDirectory = curDirrectory + @"\input";
            string[] files = System.IO.Directory.GetFiles(
                inputDirectory, "*", System.IO.SearchOption.TopDirectoryOnly);
            var outputDir = inputDirectory + @"\output\";

            List<GetMovieContext> contexts = new List<GetMovieContext>();
            foreach (var file in files)
            {
                var context = 
                    new GetMovieContext(file, 
                    outputDir, 
                    0, 
                    0);
                contexts.Add(context);
            }
            return contexts;
        }

        private static void CreatePartMovie(List<MovieInfomation> mInfoList)
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
        private static List<string> CreatePartMovies(MovieInfomation mInfo)
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

        static readonly string FfmpegPath = @"C:\root\shortcut\ffmpeg.exe";
        public static List<MovieInfomation> GetAllMoviePartsList(List<GetMovieContext> movieContexts)
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
        private static List<SoundPartInfo> CreatePartList(GetMovieContext getMovieContext)
        {
            var arguments = $@"-i {getMovieContext.InputMoviePath} -af silencedetect=noise=-30dB:d=0.4 -f null -";
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

                soundPartInfoList.Add(new SoundPartInfo(num++, startEndTimeList[i - 1], startEndTimeList[i]));
            }

            return soundPartInfoList;
        }

        private void BtnJoin_Click(object sender, RoutedEventArgs e)
        {
            ExecuteJoinPartMovies();
            ShowCompleteDialog();
        }

        private void ExecuteJoinPartMovies()
        {
            var contexts = CreateMovieContexts();
            var curDirrectory = System.IO.Directory.GetCurrentDirectory();
            var outputDir = curDirrectory + @"\output\";
            foreach (var context in contexts)
            {
                var summaryPath = GetOutPutFileSummaryPath(context.OutputDirectoryPath);

                //var arguments = $@"-f concat -i {summaryPath} -c copy {outputDir}Summary.mp4";
                var arguments =
                    $@"-safe 0 -f concat -i {summaryPath} -c:v copy -c:a copy -map 0:v -map 0:a {context.OutputDirectoryPath}\complete.mp4";
                FfmpegExecute(arguments);
            }
        }

        private void ButtonThumbnail_Click(object sender, RoutedEventArgs e)
        {
            ExecuteMakeThumbnail();
            ShowCompleteDialog();
        }

        private void ExecuteMakeThumbnail()
        {
            var tmpIntervalTime = this.intervalTime.Text;
            var parseRet = double.TryParse(tmpIntervalTime, out double intervalTime);
            if (parseRet)
            {
                intervalTime = 1 / intervalTime;
            }
            else
            {
                intervalTime = 1;
            }

            var contexts = CreateMovieContexts();

            foreach (var context in contexts)
            {
                // 出力先のフォルダ
                var outPutPath =
                    $@"{context.OutputDirectoryPath}\thumbnail";
                // 出力先のフォルダを作成
                Directory.CreateDirectory(outPutPath);

                var arguments =
                    $@"-i {context.InputMoviePath} -filter:v fps=fps={intervalTime}:round=down -q:v 0.2 {outPutPath}\%09d.jpg";

                FfmpegExecute(arguments);
            }
        }

        /// <summary>
        /// ffmpegのプロセスをスタート
        /// </summary>
        /// <param name="arguments"></param>
        /// <returns></returns>
        private static string FfmpegExecute(string arguments)
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

        private void BtnAllExecute_Click(object sender, RoutedEventArgs e)
        {
            ExecuteSeparation();
            ExecuteMakeThumbnail();
            ExecuteJoinPartMovies();
            ShowCompleteDialog();
        }

        private void FileOpenButton_Click(object sender, RoutedEventArgs e)
        {
            string initialFileName = "SelectFolder Or Files";
            var dialog = new OpenFileDialog()
            {
                FileName = initialFileName, 
                CheckFileExists = false,
                CheckPathExists = true,
                Title = "ファイルを開く",
                Filter = "全てのファイル(*.*)|*.*",
                Multiselect = true
            };
            if (dialog.ShowDialog() == true)
            {
                if (dialog.SafeFileName == initialFileName)
                {
                    this.textBlockFileName.Text = Path.GetDirectoryName(dialog.FileName);
                    return;
                }
                this.textBlockFileName.Text = string.Join(Environment.NewLine, dialog.SafeFileNames);
            }
            else
            {
                this.textBlockFileName.Text = "キャンセルされました";
            }
        }
    }
}
