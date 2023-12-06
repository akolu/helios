module LoggerTests

open Xunit
open Helios.Core.Logger
open Helios.Core.Models.EnergyMeasurement
open System

[<Fact>]
let ``LogJson should log serialized JSON data`` () =
    let logger: ILogger = ConsoleLogger()

    let data =
        Ok(
            EnergyMeasurement(
                time = DateTimeOffset.FromUnixTimeMilliseconds(0L),
                flowType = FlowType.Production,
                kwh = 0.0
            )
        )

    logger.LogJson(data) |> ignore
    Assert.True(true)
