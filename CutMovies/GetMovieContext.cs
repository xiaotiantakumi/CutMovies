namespace CutMovies
{
    public class GetMovieContext
    {
        public GetMovieContext(string inputMoviePath, string outputDirectoryPath, double startDelayDuration, double endDelayDuration)
        {
            InputMoviePath = inputMoviePath;
            OutputDirectoryPath = outputDirectoryPath;
            StartDelayDuration = startDelayDuration;
            EndDelayDuration = endDelayDuration;
        }

        public string InputMoviePath { get; private set; }
        public string OutputDirectoryPath { get; private set; }

        public double StartDelayDuration { get; }
        public double EndDelayDuration { get; }
    }
}