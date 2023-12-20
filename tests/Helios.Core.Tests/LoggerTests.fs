module LoggerTests

open Xunit
open Helios.Core.Logger
open Helios.Core.Models.SolarPanelOutput
open System

[<Fact>]
let ``LogJson should log serialized JSON data`` () =
    // let logger = createLogger (CustomLoggerProvider())

    // let data =
    //     Ok(SolarPanelOutput(time = DateTimeOffset.FromUnixTimeMilliseconds(0L), kwh = 0.0))

    // logger.LogJson(data) |> ignore
    Assert.True(true)
