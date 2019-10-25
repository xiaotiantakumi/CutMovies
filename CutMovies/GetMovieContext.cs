using System.IO;

namespace CutMovies
{
    public class GetMovieContext
    {
        public GetMovieContext(string inputMoviePath, string outputDirectoryPath, double startDelayDuration, double endDelayDuration, string createTimeCondition)
        {
            InputMoviePath = inputMoviePath;
            StartDelayDuration = startDelayDuration;
            EndDelayDuration = endDelayDuration;

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
    }
}