open System

open System.IO
open System.Threading
open Binance.Net.Interfaces.Clients.GeneralApi
open Binance.Net.Objects.Models.Spot.Socket
open CryptoExchange.Net.Sockets
open Lola
open Lola.StateMachine
open Lola.TradingEngine
open Lola.AlphaEngine
open FSharp.Linq
open Lola.TradingEngine
open Microsoft.FSharp.Control




[<EntryPoint>]
let main argv =
    let userStreamKey =
        BinanceTrading.getUserDataStreamKey ()
        |> Async.RunSynchronously

    let mutable (state: State) = Default

    let lastPrice =
        BinanceTrading.client.SpotApi.ExchangeData.GetPriceAsync("BTCUSDT")
        |> Async.AwaitTask
        |> Async.RunSynchronously

    LolaTrader.amount <- Decimal.Round(800.0m / lastPrice.Data.Price, 5, MidpointRounding.ToZero)

    let orderUpdateHandler (update: DataEvent<BinanceStreamOrderUpdate>) =
        Console.WriteLine(
            $"{DateTime.Now}\t[ORDER UPDATE] {update.Data.ExecutionType} {update.Data.Id} {update.Data.Price} {update.Data.Quantity}"
        )

        let event =
            LolaTrader.handleOrderUpdate update.Data

        Console.Write($"{DateTime.Now}\t[ORDER UPDATE] PRE-STATE: {state}")
        state <- LolaTrader.transition state event
        Console.WriteLine($"{DateTime.Now}\t[ORDER UPDATE] POST-STATE: {state}")
        LolaTrader.handleSideEffect state

    let newCloseHandler (close: decimal) =
        Console.WriteLine($"{DateTime.Now}\t[NEW CLOSE] {close} {state}")
        let event = LolaTrader.handleNewClose close

        Console.Write($"{DateTime.Now}\t[CLOSE UPDATE] PRE-STATE: {state}")
        state <- LolaTrader.transition state event
        Console.WriteLine($"{DateTime.Now}\t[CLOSE UPDATE] POST-STATE: {state}")
        LolaTrader.handleSideEffect state

    let balanceUpdateHandler m = m |> ignore

    let userStreamSub =
        StreamListeners.userDataSub userStreamKey orderUpdateHandler balanceUpdateHandler

    let klineStreamSub =
        StreamListeners.klineSub newCloseHandler

    // fill model with previous closes
    let closes =
        BinanceTrading.getLastFiveCloses
        |> Async.RunSynchronously

    for c in closes do
        AutoRegressionModel.NewClose c


    [| userStreamSub; klineStreamSub |]
    |> Async.Parallel
    |> Async.Ignore
    |> Async.RunSynchronously

    Console.Read() |> ignore
    0
