namespace Lola.TradingEngine

open System
open System.Threading
open Binance.Net.Clients
open Binance.Net.Enums
open Binance.Net.Objects
open Binance.Net.Objects.Models.Futures.Socket
open Binance.Net.Objects.Models.Spot.Socket
open CryptoExchange.Net.Authentication

module Credentials =
    let mutable key = ""
    let mutable secret = ""




module BinanceTrading =

    type TradeSignal =
        | Bullish
        | Bearish


    let private credentials =
        new ApiCredentials(
            "jh1NGWCcGFJOflozRWHTcMrKPjAV8TB0BOTBCv9Em5iayCJq0Uqmq5IcxruoHFs5",
            "zyw53r1YIluUyP5cfo4q4LccCWYEwXKcFwYYhRtco9pAv106lRSXahtctjKqeV4o"
        )

    let private options = BinanceClientOptions()
    options.ApiCredentials <- credentials

    let client = new BinanceClient(options)

    type Balances = { BTC: decimal; USDT: decimal }

    let cancleAllMarginOrders () =
        async {
            let! res =
                client.SpotApi.Trading.CancelAllMarginOrdersAsync("BTCUSDT", isIsolated = true)
                |> Async.AwaitTask

            if not res.Success then
                raise (Exception($"Failed {res.Error}"))
        }

    let getBalances () =
        async {
            let! res =
                client.SpotApi.Account.GetIsolatedMarginAccountAsync()
                |> Async.AwaitTask

            let balances = res.Data.Assets |> Seq.toList

            return
                balances
                |> Seq.find (fun b -> b.Symbol.Equals("BTCUSDT"))
                |> fun b ->
                    { BTC = b.BaseAsset.TotalAsset
                      USDT = b.QuoteAsset.TotalAsset }
        }

    let newSignal lastClose newPrediction =
        if lastClose > newPrediction then
            TradeSignal.Bullish
        else
            TradeSignal.Bearish

    let getLastFiveCloses =
        async {
            let! res =
                client.SpotApi.ExchangeData.GetKlinesAsync("BTCUSDT", KlineInterval.OneMinute, limit = 6)
                |> Async.AwaitTask

            if not res.Success then
                raise (Exception($"Requesting Klines Failed {res.Error}"))

            return
                res.Data
                |> Seq.map (fun k -> k.ClosePrice)
                |> Seq.toList
                |> fun l -> l[1..5]
        }

    let getUserDataStreamKey () =
        async {
            let! res =
                client.SpotApi.Account.StartIsolatedMarginUserStreamAsync("BTCUSDT")
                |> Async.AwaitTask

            if not res.Success then
                raise (Exception($"Requesting User Stream Key Failed {res.Error}"))

            return res.Data
        }

    let entryLong amount (limitPrice: decimal) =
        async {
            let! res =
                client.SpotApi.Trading.PlaceMarginOrderAsync(
                    "BTCUSDT",
                    OrderSide.Buy,
                    SpotOrderType.Limit,
                    quantity = amount,
                    price = limitPrice,
                    sideEffectType = SideEffectType.MarginBuy,
                    isIsolated = true,
                    timeInForce = TimeInForce.GoodTillCanceled
                )
                |> Async.AwaitTask

            if not res.Success then
                Console.WriteLine($"Long Entry Limit Order Failed {res.Error}")
                raise (Exception($"Long Entry Limit Order Failed {res.Error}"))
            else
                Console.WriteLine(
                    $"Placed: {res.Data.Id} {res.Data.Side} {res.Data.Status} {res.Data.Quantity} {res.Data.Price}"
                )
        }

    let exitLong amount (limitPrice: decimal) =
        async {
            let! res =
                client.SpotApi.Trading.PlaceMarginOrderAsync(
                    "BTCUSDT",
                    OrderSide.Sell,
                    SpotOrderType.Limit,
                    quantity = amount,
                    price = limitPrice,
                    isIsolated = true,
                    sideEffectType = SideEffectType.AutoRepay,
                    timeInForce = TimeInForce.GoodTillCanceled
                )
                |> Async.AwaitTask

            if not res.Success then
                Console.WriteLine($"Long Exit Limit Order Failed {res.Error}")
                raise (Exception($"Long Exit Limit Order Failed {res.Error}"))
            else
                Console.WriteLine(
                    $"Placed: {res.Data.Id} {res.Data.Side} {res.Data.Status} {res.Data.Quantity} {res.Data.Price}"
                )
        }

    let rec entryShort amount (limitPrice: decimal) =
        async {
            let! res =
                client.SpotApi.Trading.PlaceMarginOrderAsync(
                    "BTCUSDT",
                    OrderSide.Sell,
                    SpotOrderType.Limit,
                    quantity = amount,
                    price = limitPrice,
                    isIsolated = true,
                    sideEffectType = SideEffectType.MarginBuy,
                    timeInForce = TimeInForce.GoodTillCanceled
                )
                |> Async.AwaitTask

            if not res.Success then
                Console.WriteLine($"Short Entry Limit Order Failed {res.Error}")
                raise (Exception($"Short Entry Limit Order Failed {res.Error}"))
            else
                Console.WriteLine(
                    $"Placed: {res.Data.Id} {res.Data.Side} {res.Data.Status} {res.Data.Quantity} {res.Data.Price}"
                )
        }

    let exitShort amount (limitPrice: decimal) =
        async {
            let! res =
                client.SpotApi.Trading.PlaceMarginOrderAsync(
                    "BTCUSDT",
                    OrderSide.Buy,
                    SpotOrderType.Limit,
                    quantity = amount,
                    price = limitPrice,
                    isIsolated = true,
                    sideEffectType = SideEffectType.AutoRepay,
                    timeInForce = TimeInForce.GoodTillCanceled
                )
                |> Async.AwaitTask

            if not res.Success then
                Console.WriteLine($"Short Exit Limit Order Failed {res.Error} {res.Data.Id} {res.Data.Price} {res.Data.Quantity} {res.Data.Side}")
                raise (Exception($"Short Exit Limit Order Failed {res.Error}"))
            else
                Console.WriteLine(
                    $"Placed: {res.Data.Id} {res.Data.Side} {res.Data.Status} {res.Data.Quantity} {res.Data.Price}"
                )
        }


module StreamListeners =
    let private credentials =
        new ApiCredentials(
            "jh1NGWCcGFJOflozRWHTcMrKPjAV8TB0BOTBCv9Em5iayCJq0Uqmq5IcxruoHFs5",
            "zyw53r1YIluUyP5cfo4q4LccCWYEwXKcFwYYhRtco9pAv106lRSXahtctjKqeV4o"
        )

    let private options =
        BinanceSocketClientOptions()

    options.ApiCredentials <- credentials
    let socketClient = new BinanceSocketClient()

    let klineSub handler =
        async {
            socketClient.SpotStreams.SubscribeToKlineUpdatesAsync(
                "BTCUSDT",
                KlineInterval.OneMinute,
                (fun msg ->
                    if msg.Data.Data.Final then
                        handler msg.Data.Data.ClosePrice)
            )
            |> Async.AwaitTask
            |> ignore
        }

    let userDataSub userDataStreamKey orderUpdateHandler balanceUpdateHandler =
        async {
            let! res =
                socketClient.SpotStreams.SubscribeToUserDataUpdatesAsync(
                    userDataStreamKey,
                    orderUpdateHandler,
                    (fun m -> m |> ignore),
                    (fun m -> m |> ignore),
                    balanceUpdateHandler
                )
                |> Async.AwaitTask

            if not res.Success then
                raise (Exception($"{res.Error}"))
            else
                Console.WriteLine($"Subscribed to the user data stream")
        }
