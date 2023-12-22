module FingridTests

open Xunit
open Helios.Core.DataProviders.CsvParsers.FingridParser
open FSharp.Data
open System
open Helios.Core.Logger

let TOLERANCE = 0.000001

let csvString =
    """
Mittauspisteen tunnus;Tuotteen tyyppi;Resoluutio;Yksikkötyyppi;Lukeman tyyppi;Alkuaika;Määrä;Laatu
12345;8716867000030;PT1H;kWh;BN01;2023-09-23T08:00:00Z;0,023000;OK
12345;8716867000030;PT1H;kWh;BN01;2023-09-23T09:00:00Z;0,015000;OK
12345;8716867000030;PT1H;kWh;BN01;2023-09-23T10:00:00Z;0,017000;OK
12345;8716867000030;PT1H;kWh;BN01;2023-09-23T11:00:00Z;0,136000;OK
12345;8716867000030;PT1H;kWh;BN01;2023-09-23T12:00:00Z;0,020000;OK
12345;8716867000030;PT1H;kWh;BN01;2023-09-23T13:00:00Z;0,041000;OK
12345;8716867000030;PT1H;kWh;BN01;2023-09-23T14:00:00Z;0,008000;OK
12345;8716867000030;PT1H;kWh;BN01;2023-09-23T15:00:00Z;0,002000;OK
12345;8716867000030;PT1H;kWh;BN01;2023-09-23T16:00:00Z;0,000000;OK
98765;8716867000030;PT1H;kWh;BN01;2023-09-23T08:00:00Z;0,174000;OK
98765;8716867000030;PT1H;kWh;BN01;2023-09-23T09:00:00Z;0,199000;OK
98765;8716867000030;PT1H;kWh;BN01;2023-09-23T10:00:00Z;0,209000;OK
98765;8716867000030;PT1H;kWh;BN01;2023-09-23T11:00:00Z;0,072000;OK
98765;8716867000030;PT1H;kWh;BN01;2023-09-23T12:00:00Z;1,326000;OK
98765;8716867000030;PT1H;kWh;BN01;2023-09-23T13:00:00Z;0,247000;OK
98765;8716867000030;PT1H;kWh;BN01;2023-09-23T14:00:00Z;0,422000;OK
98765;8716867000030;PT1H;kWh;BN01;2023-09-23T15:00:00Z;0,529000;OK
98765;8716867000030;PT1H;kWh;BN01;2023-09-23T16:00:00Z;0,602000;OK
"""

[<Fact>]
let ``parseNetEnergyConsumptionFromDatahubCsv parses csv and calculates net energy consumption by time`` () =
    let result =
        parseNetEnergyConsumptionFromDatahubCsv
            (CsvFile.Parse(csvString, ";"))
            (Fingrid.Init
                { Logger = createLogger (new HeliosLoggerProvider(LoggerOptions.None))
                  SiteIdentifiers =
                    { Production = "12345"
                      Consumption = "98765" } })

    let expected: FingridReading list =
        [ { Time = DateTimeOffset.Parse("2023-09-23T08:00:00Z")
            Consumption = 0.174
            Production = 0.023 }
          { Time = DateTimeOffset.Parse("2023-09-23T09:00:00Z")
            Consumption = 0.199
            Production = 0.015 }
          { Time = DateTimeOffset.Parse("2023-09-23T10:00:00Z")
            Consumption = 0.209
            Production = 0.017 }
          { Time = DateTimeOffset.Parse("2023-09-23T11:00:00Z")
            Consumption = 0.072
            Production = 0.136 }
          { Time = DateTimeOffset.Parse("2023-09-23T12:00:00Z")
            Consumption = 1.326
            Production = 0.02 }
          { Time = DateTimeOffset.Parse("2023-09-23T13:00:00Z")
            Consumption = 0.247
            Production = 0.041 }
          { Time = DateTimeOffset.Parse("2023-09-23T14:00:00Z")
            Consumption = 0.422
            Production = 0.008 }
          { Time = DateTimeOffset.Parse("2023-09-23T15:00:00Z")
            Consumption = 0.529
            Production = 0.002 }
          { Time = DateTimeOffset.Parse("2023-09-23T16:00:00Z")
            Consumption = 0.602
            Production = 0.0 } ]

    // can't compare the lists directly due to floats, so compare each item with tolerance on floats
    for (expected, actual) in List.zip expected result do
        Assert.Equal(expected.Time, actual.Time)
        Assert.InRange(actual.Consumption, expected.Consumption - TOLERANCE, expected.Consumption + TOLERANCE)
        Assert.InRange(actual.Production, actual.Production - TOLERANCE, actual.Production + TOLERANCE)
