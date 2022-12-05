namespace LolaObserver.Data;

public class LimitQueue<T>
{
    private Queue<T> _queue;
    private int _limit;


    public LimitQueue(int limit)
    {
        _limit = limit;
        _queue = new Queue<T>();
    }

    public void Enqueue(T value)
    {
        if (_queue.ToList().Count == _limit)
            _queue.Dequeue();
        _queue.Enqueue(value);
    }

    public List<T> ToList()
    {
        return _queue.ToList();
    }
}