module LolaTests

open System
open NUnit.Framework
open Lola.AlphaEngine

[<SetUp>]
let Setup () = ()

[<Test>]
let TestPredictionValue () =
    // most recent is last
    let closes =
        [| 16493.95m
           16490.58m
           16486.07m
           16483.61m
           16474.03m |]

    let expectedPrediction = 16474.08419m
    
    for close in closes do
        AutoRegressionModel.NewClose(close)
    
    Assert.That(Decimal.Round(AutoRegressionModel.Predict(), 5), Is.EqualTo(expectedPrediction))
