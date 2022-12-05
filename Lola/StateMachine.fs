namespace Lola

open System
open CryptoExchange.Net.CommonObjects
open Lola.TradingEngine.BinanceTrading

module StateMachine =
    type Signal =
        | Bullish of Close: decimal
        | Bearish of Close: decimal


    type State =
        | Default
        | EntryReset of Signal
        | ExitReset of PreviousSignal: Signal * NewSignal: Signal
        | PlaceExit of PreviousSignal: Signal * ExitPrice: decimal * newSignal: Signal
        | ExitPlaced of Signal
        | ExitFilled of Signal
        | PlaceEntry of Signal
        | EntryPlaced of Signal
        | EntryFilled of Signal

    type Event =
        | NewSignal of Signal
        | OrderPlaced
        | OrderPartialFill
        | OrderFilled
        | OrderCanceled
