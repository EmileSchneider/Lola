using Binance.Net.Clients;
using Binance.Net.Objects.Models.Spot.Socket;
using CryptoExchange.Net.Sockets;
using MarketWatch;

class Program
{
    private static BinanceClient _client;
    private static BinanceSocketClient _socket;

    private static LimitQueue<BinanceStreamTrade> _trades50 = new(50);
    private static LimitQueue<BinanceStreamTrade> _trades100 = new(100);
    private static LimitQueue<BinanceStreamTrade> _trades250 = new(250);
    private static LimitQueue<BinanceStreamTrade> _trades500 = new(500);
    private static LimitQueue<BinanceStreamTrade> _trades1000 = new(1000);
    private static LimitQueue<BinanceStreamTrade> _trades5000 = new(5000);
    private static decimal BidPrice;
    private static decimal AskPrice;
    private static decimal Spread;
    private static decimal Imbalance;

    public async static Task Main(string[] args)
    {
        _client = new BinanceClient();
        _socket = new BinanceSocketClient();
        await NewSubscription(OnMessage);
        Console.Read();
    }

    public static void OnMessage(DataEvent<BinanceStreamTrade> msg)
    {
        Console.Clear();
        _trades50.Enqueue(msg.Data);
        _trades100.Enqueue(msg.Data);
        _trades250.Enqueue(msg.Data);
        _trades500.Enqueue(msg.Data);
        _trades1000.Enqueue(msg.Data);
        _trades5000.Enqueue(msg.Data);

        Console.SetCursorPosition(0, 0);
        PrintInfos(_trades50, 50);
        PrintInfos(_trades100, 100);
        PrintInfos(_trades250, 250);
        PrintInfos(_trades500, 500);
        PrintInfos(_trades1000, 1000);
        PrintInfos(_trades5000, 5000);
    }

    public static void PrintInfos(LimitQueue<BinanceStreamTrade> trades, int size)
    {
        var l = trades.ToList();
        List<decimal> distance = new();
        for (int i = 0; i < l.Count - 1; i++)
        {
            distance.Add(Math.Abs(l[i].Price - l[i+1].Price));
        }

        List<TimeSpan> time = new();
        for (int i = 0; i < l.Count - 1; i++)
        {
            time.Add(l[i].TradeTime - l[i+1].TradeTime);
        }

        TimeSpan startEnd = l.Last().TradeTime - l.First().TradeTime;
        
        Console.WriteLine(
            $"Average {size}\t Price:{Math.Round(distance.Average(), 5)} Max: {distance.Max()} Size: {Math.Round(l.Average(c => c.Quantity), 5)}: TimeSpan: {Math.Abs(Math.Round(time.Average(c => c.TotalMilliseconds), 2))} Distance {Math.Round(startEnd.TotalMilliseconds, 2)}");
    }

    private async static Task NewSubscription(Action<DataEvent<BinanceStreamTrade>> onMessage)
    {
        _ = await _socket.SpotStreams.SubscribeToTradeUpdatesAsync("BTCUSDT", onMessage);
    }
}