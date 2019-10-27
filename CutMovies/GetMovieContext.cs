using System.IO;

namespace CutMovies
{
    public class GetMovieContext
    {
        public GetMovieContext(string inputMoviePath, string outputDirectoryPath, double startDelayDuration, double endDelayDuration, string createTimeCondition, double intervalTime, string noSoundLevel, string noSoundTerm)
        {
            InputMoviePath = inputMoviePath;
            StartDelayDuration = startDelayDuration;
            EndDelayDuration = endDelayDuration;
            IntervalTime = intervalTime;
            NoSoundLevel = noSoundLevel;
            NoSoundTerm = noSoundTerm;

            CreateTimeCondition = double.TryParse(createTimeCondition, out double ret) ? ret : 0;
            ImportFileName = Path.GetFileNameWithoutExtension(InputMoviePath);
            OutputDirectoryPath = outputDirectoryPath + ImportFileName;
        }

        public string ImportFileName { get; }

        public string InputMoviePath { get; }
        public string OutputDirectoryPath { get;}
        public double CreateTimeCondition { get; }

        public double StartDelayDuration { get; }
        public double EndDelayDuration { get; }
        public double IntervalTime { get; }

        public string NoSoundLevel { get; }
        public string NoSoundTerm { get; }
    }
}