namespace OnlineVideos.Sites.Ard
{
    public class Result<T>
    {
        public Result(T value, ContinuationToken continuationToken)
        {
            Value = value;
            ContinuationToken = continuationToken;
        }

        public T Value { get; private set; }
        public ContinuationToken ContinuationToken { get; private set; }
    }
}
