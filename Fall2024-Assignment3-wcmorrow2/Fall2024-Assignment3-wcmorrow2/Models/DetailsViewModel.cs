namespace Fall2024_Assignment3_wcmorrow2.Models
{
    public class DetailsViewModel<T>
    {
        public T Value { get; set; }
        public List<object?> ValueList { get; set; }
    }
    public class ActorDetailsViewModel : DetailsViewModel<Actor>
    {
        public List<Movie?> Movies { get; set; }
    }
    public class MovieDetailsViewModel : DetailsViewModel<Movie>
    {
        public List<Actor?>? Actors { get; set; }
        // lists the review and the sentiment analysis
        public List<(string, string)?>? Reviews { get; set; }
    }

}
