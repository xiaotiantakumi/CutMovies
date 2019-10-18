using System.Collections.Generic;
using System.IO;

namespace CutMovies
{
    public class MovieInfomation
    {
        public string FromFileName { get; set; }
        public GetMovieContext Context { get; set; }

        public List<SoundPartInfo> PartsData { get; set; }

        public MovieInfomation(GetMovieContext context, List<SoundPartInfo> partsData)
        {
            FromFileName = Path.GetFileNameWithoutExtension(context.InputMoviePath);
            Context = context;
            PartsData = partsData;
        }
    }
}