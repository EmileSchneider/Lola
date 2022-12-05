namespace Lola.AlphaEngine

open System
open System.Collections.Generic

module AutoRegressionModel =
    let private queue = Queue<decimal>()

    let private constant = 0.110219m

    let private weights =
        [| 1.015592m
           -0.039831m
           0.013372m
           0.005618m
           0.005247m |]

    let NewClose (close: decimal) =
        if queue.Count = 5 then
            queue.Dequeue() |> ignore

        queue.Enqueue(close)


    let Predict () =
        let values =
            queue.ToArray() |> Array.toList |> List.rev

        constant
        + weights[0] * values[0]
        + weights[1] * values[1]
        + weights[2] * values[2]
        + weights[3] * values[3]
        + weights[4] * values[4]
