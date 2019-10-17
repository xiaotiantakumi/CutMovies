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
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Path = System.IO.Path;

namespace CutMovies
{
    public class MovieInfomation
    {
        public string FromFileName { get; set; }
        public GetMovieContext Context { get; set; }

        public List<MoviePartsData> PartsData { get; set; }

        public MovieInfomation(GetMovieContext context, List<MoviePartsData> partsData)
        {
            FromFileName = Path.GetFileNameWithoutExtension(context.InputMoviePath);
            Context = context;
            PartsData = partsData;
        }
    }

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
                var context = new GetMovieContext(file, outputDir, 0.5, 0);
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

        private static void CreatePartMoviesByParallel()
        {
            //ParallelOptions option = new ParallelOptions();
            //option.MaxDegreeOfParallelism = 2;
            //Parallel.ForEach(mInfoList, (duration) =>
            //{
            //    if (!duration.IsCreate)
            //    {
            //        return;
            //    }

            //    var outputFileName = context.OutputDirectoryPath + "output" + duration.Number + ".mp4";
            //    var arguments =
            //        $@"-ss {duration.From - 0.5} -i {context.InputMoviePath} -t {duration.Duration + 0.5}  {outputFileName}";
            //    using var process = new Process();
            //    process.StartInfo = new ProcessStartInfo
            //    {
            //        FileName = FfmpegPath,
            //        Arguments = arguments,
            //        CreateNoWindow = true,
            //        UseShellExecute = false,
            //        RedirectStandardOutput = true,
            //        RedirectStandardError = true
            //    };
            //    process.Start();
            //    process.StandardError.ReadLine();
            //});
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

        private static List<MoviePartsData> CreatePartList(GetMovieContext getMovieContext)
        {
            var arguments = $@"-i {getMovieContext.InputMoviePath} -af silencedetect=noise=-30dB:d=0.4 -f null -";

            string rawinfo = string.Empty;
            using (var process = new Process())
            {
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

                // StandardErrorに情報が入る
                rawinfo = process.StandardError.ReadToEnd();
                process.WaitForExit();
                if (process.HasExited)
                {
                    process.Kill();
                }
            }

            var tmpInfo = rawinfo.Replace(Environment.NewLine, " ").Split(' ');

            List<MoviePartsData> durationList = new List<MoviePartsData>();
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

                durationList.Add(new MoviePartsData(num++, startEndTimeList[i - 1], startEndTimeList[i]));
            }

            return durationList;
        }

        private void BtnJoin_Click(object sender, RoutedEventArgs e)
        {
            var curDirrectory = System.IO.Directory.GetCurrentDirectory();
            var outputDir = curDirrectory + @"\output\";
            var summaryPath = GetOutPutFileSummaryPath(outputDir);

            using var process = new Process();
            //var arguments = $@"-f concat -i {summaryPath} -c copy {outputDir}Summary.mp4";
            var arguments = $@"-safe 0 -f concat -i {summaryPath} -c:v copy -c:a copy -map 0:v -map 0:a output.mp4";

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
            //var tmp2 = process.StandardOutput.ReadToEnd();
            process.WaitForExit();
            if (process.HasExited)
            {
                process.Kill();
            }

            Dialog dialog = new Dialog();
            dialog.Owner = this;
            dialog.Show();
        }

        private void ButtonThumbnail_Click(object sender, RoutedEventArgs e)
        {
            var intervalTime = this.intervalTime.Text;
            var contexts = CreateMovieContexts();

            foreach (var context in contexts)
            {
                // 出力先のフォルダ
                var outPutPath = $@"{context.OutputDirectoryPath}{Path.GetFileNameWithoutExtension(context.InputMoviePath)}";
                // 出力先のフォルダを作成
                Directory.CreateDirectory(outPutPath);

                var arguments =
                    $@"-i {context.InputMoviePath} -vf thumbnail -frames:v 10 -vsync 0 {outPutPath}\thumbnail%03d.jpg";
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
            }
            //
        }
    }
}
