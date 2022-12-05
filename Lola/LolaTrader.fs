namespace Lola

open System
open Binance.Net.Enums
open Binance.Net.Objects.Models.Spot.Socket
open CryptoExchange.Net.CommonObjects
open CryptoExchange.Net.Sockets
open Lola.AlphaEngine
open Lola.StateMachine
open Lola.TradingEngine
open Microsoft.FSharp.Control

module LolaTrader =

    let mutable amount = 0m

    let handleNewClose (close: decimal) : StateMachine.Event =
        AutoRegressionModel.NewClose(close)

        let prediction =
            AutoRegressionModel.Predict()

        if prediction > close then
            Event.NewSignal(Signal.Bearish close)
        else
            Event.NewSignal(Signal.Bullish close)

    let handleOrderUpdate (update: BinanceStreamOrderUpdate) : StateMachine.Event =
        match update.ExecutionType with
        | ExecutionType.New -> OrderPlaced
        | ExecutionType.Trade ->
            if update.Quantity = update.QuantityFilled then
                OrderFilled
            else
                OrderPartialFill
        | ExecutionType.Canceled -> OrderCanceled

    let transition state event =
        match state with
        | Default ->
            match event with
            | NewSignal signal -> PlaceEntry(signal)
            | OrderPlaced
            | OrderPartialFill
            | OrderFilled -> raise (Exception("SOMETHING WENT WRONG"))

        | PlaceExit (previousSignal, exitPrice, signal) ->
            match event with
            | OrderPlaced -> ExitPlaced signal
            | NewSignal _
            | OrderPartialFill
            | OrderFilled -> raise (Exception("SOMETHING WENT WRONG"))

        | ExitPlaced signal ->
            match event with
            | OrderPartialFill -> ExitPlaced signal
            | OrderFilled -> PlaceEntry signal
            | NewSignal newSignal -> ExitReset(signal, newSignal)
            | OrderPlaced -> raise (Exception("WRONG"))

        | PlaceEntry signal ->
            match event with
            | OrderPlaced -> EntryPlaced signal
            | OrderFilled -> EntryFilled signal
            | NewSignal _
            | OrderPartialFill -> raise (Exception("SOMETHING WENT WRONG"))

        | EntryPlaced signal ->
            match event with
            | OrderPartialFill -> EntryPlaced signal
            | OrderFilled -> EntryFilled signal
            | NewSignal newSignal -> EntryReset newSignal
            | OrderPlaced -> raise (Exception("WRONG"))

        | EntryFilled previousSignal ->
            match event with
            | NewSignal signal ->
                match signal with
                | Bullish close
                | Bearish close -> PlaceExit(previousSignal, close, signal)
            | OrderPlaced
            | OrderPartialFill
            | OrderFilled -> raise (Exception("WROGN@"))

        | ExitFilled signal ->
            match event with
            | OrderCanceled
            | OrderPlaced -> PlaceEntry signal
            | OrderFilled
            | OrderPartialFill
            | NewSignal _ -> raise (Exception("WRONG@@@@"))

        | ExitReset (previousSignal, signal) ->
            match event with
            | OrderFilled -> PlaceEntry signal
            | OrderCanceled ->
                match signal with
                | Bullish close -> PlaceExit(previousSignal, close, signal)
                | Bearish close -> PlaceExit(previousSignal, close, signal)
            | _ -> raise (Exception("asdf"))
        | EntryReset signal ->
            match event with
            | OrderCanceled -> PlaceEntry signal
            | _ -> raise (Exception("WRONG"))

    let handleSideEffect state =
        match state with
        | PlaceExit (previousSignal, exitPrice, signal) ->
            match previousSignal with
            | Bullish _ ->
                BinanceTrading.exitLong amount exitPrice
                |> Async.StartImmediate
            | Bearish _ ->
                BinanceTrading.exitShort amount exitPrice
                |> Async.StartImmediate

        | PlaceEntry signal ->
            match signal with
            | Bullish close ->
                BinanceTrading.entryLong amount close
                |> Async.StartImmediate
            | Bearish close ->
                BinanceTrading.entryShort amount close
                |> Async.StartImmediate

        | EntryReset signal ->
            BinanceTrading.cancleAllMarginOrders ()
            |> Async.StartImmediate
        | ExitReset (previousSignal, signal) ->
            BinanceTrading.cancleAllMarginOrders ()
            |> Async.RunSynchronously
        | _ -> 0 |> ignore
