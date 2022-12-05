namespace MarketWatch;

public class LimitQueue<T>
{
    private readonly Queue<T> _queue = new();
    private readonly int _limit;

    public LimitQueue(int limit)
    {
        _limit = limit;
    }

    public void Enqueue(T value)
    {
        while (_queue.Count >= _limit)
        {
            _queue.Dequeue();
        }
        _queue.Enqueue(value);
    }

    public List<T> ToList()
    {
        return _queue.ToList();
    }
}