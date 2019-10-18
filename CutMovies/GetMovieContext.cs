using System.IO;

namespace CutMovies
{
    public class GetMovieContext
    {
        public GetMovieContext(string inputMoviePath, string outputDirectoryPath, double startDelayDuration, double endDelayDuration)
        {
            InputMoviePath = inputMoviePath;
            StartDelayDuration = startDelayDuration;
            EndDelayDuration = endDelayDuration;
            ImportFileName = Path.GetFileNameWithoutExtension(InputMoviePath);
            OutputDirectoryPath = outputDirectoryPath + ImportFileName;
        }

        public string ImportFileName { get; }

        public string InputMoviePath { get; }
        public string OutputDirectoryPath { get;}

        public double StartDelayDuration { get; }
        public double EndDelayDuration { get; }
    }
}