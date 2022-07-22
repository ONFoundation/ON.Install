namespace ON.Content.Video.Rumble.Service.Models
{
    // From Martin Fowler: https://martinfowler.com/eaaDev/Range.html
    public interface IRange<T>
    {
        T Start { get; }
        T End { get; }
        bool Includes(T value);
        bool Includes(IRange<T> range);
    }
}
