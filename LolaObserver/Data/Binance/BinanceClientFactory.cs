using Binance.Net.Clients;
using Binance.Net.Objects;
using CryptoExchange.Net.Authentication;

namespace LolaObserver.Data.Binance;

public class BinanceClientFactory
{
    private static BinanceClient? _client;
    private static BinanceSocketClient? _socketClient;

    public static BinanceClient NewRestClient(string apiKey, string apiSecret)
    {
        if (_client == null)
            _client = new BinanceClient(new BinanceClientOptions()
            {
                ApiCredentials = new ApiCredentials(apiKey, apiSecret)
            });

        return _client;
    }

    public static BinanceSocketClient NewSocketClient(string apiKey, string apiSecret)
    {
        if (_socketClient == null)
            _socketClient = new BinanceSocketClient(new BinanceSocketClientOptions()
            {
                ApiCredentials = new ApiCredentials(apiKey, apiSecret)
            });

        return _socketClient;
    }
}