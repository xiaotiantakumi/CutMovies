namespace CutMovies
{
    public class SoundPartInfo
    {
        public int Number { get; }
        public double From { get; }
        public double To { get; }
        public double Duration { get; }
        public bool IsCreate { get; }
        public SoundPartInfo(int num,double from, double to)
        {
            Number = num;
            From = @from;
            To = to;
            Duration = to - from;
            IsCreate = !(Duration <= 0.5);
        }
    }
}