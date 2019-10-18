﻿using System;
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
            var contexts = CreateMovieContexts();
            var info = GetAllMoviePartsList(contexts);

            CreatePartMovie(info);

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
                inputDirectory, "*", System.IO.SearchOption.AllDirectories);
            var outputDir = curDirrectory + @"\output\";

            List<GetMovieContext> contexts = new List<GetMovieContext>();
            foreach (var file in files)
            {
                var context = 
                    new GetMovieContext(file, 
                    outputDir, 
                    0.5, 
                    0);
                contexts.Add(context);
            }
            return contexts;
        }

        private static void CreatePartMovie(List<MovieInfomation> mInfoList)
        {
            foreach (var mInfo in mInfoList)
            {
                CreatePartMovies(mInfo);
            }
        }
        private static void CreatePartMovies(MovieInfomation mInfo)
        {
            var context = mInfo.Context;
            var partsData = mInfo.PartsData;
            // 出力先のフォルダ
            var outPutPath = $@"{context.OutputDirectoryPath}{mInfo.FromFileName}";
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

            var summaryPath = GetOutPutFileSummaryPath(outPutPath);
            using StreamWriter writer = new StreamWriter(summaryPath, false);
            foreach (var file in outPutFileList)
            {
                //file.Replace(@"\", @"\\");
                writer.WriteLine(@"file " + file.Replace(@"\", @"\\"));
            }
        }

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
            var curDirrectory = System.IO.Directory.GetCurrentDirectory();
            var outputDir = curDirrectory + @"\output\";
            var summaryPath = GetOutPutFileSummaryPath(outputDir);

            //var arguments = $@"-f concat -i {summaryPath} -c copy {outputDir}Summary.mp4";
            var arguments = $@"-safe 0 -f concat -i {summaryPath} -c:v copy -c:a copy -map 0:v -map 0:a output.mp4";
            FfmpegExecute(arguments);

            Dialog dialog = new Dialog();
            dialog.Owner = this;
            dialog.Show();
        }

        private void ButtonThumbnail_Click(object sender, RoutedEventArgs e)
        {
            var tmpIntervalTime = this.intervalTime.Text;
            double intervalTime = 1;
            var parseRet = double.TryParse(tmpIntervalTime, out intervalTime);
            if (parseRet)
            {
                intervalTime = 1 / intervalTime;
            }
            var contexts = CreateMovieContexts();
            
            foreach (var context in contexts)
            {
                // 出力先のフォルダ
                var outPutPath = $@"{context.OutputDirectoryPath}{Path.GetFileNameWithoutExtension(context.InputMoviePath)}\thumbnail";
                // 出力先のフォルダを作成
                Directory.CreateDirectory(outPutPath);

                var arguments =
                    $@"-i {context.InputMoviePath} -filter:v fps=fps={intervalTime}:round=down -q:v 0.2 {outPutPath}\%04d.jpg";
                
                FfmpegExecute(arguments);
            }
            Dialog dialog = new Dialog();
            dialog.Owner = this;
            dialog.Show();
            //
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
    }
}
