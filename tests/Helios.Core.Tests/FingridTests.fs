module FingridTests

open Xunit
open Helios.Core.Services
open FSharp.Data
open TestUtils
open System

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
        Fingrid.parseNetEnergyConsumptionFromDatahubCsv
            (CsvFile.Parse(csvString, ";"))
            (Fingrid.init
                { Logger = MockLogger()
                  SiteIdentifiers =
                    { Production = "12345"
                      Consumption = "98765" } })

    let expected =
        [ (DateTimeOffset.Parse("2023-09-23T08:00:00Z"), 0.151)
          (DateTimeOffset.Parse("2023-09-23T09:00:00Z"), 0.184)
          (DateTimeOffset.Parse("2023-09-23T10:00:00Z"), 0.192)
          (DateTimeOffset.Parse("2023-09-23T11:00:00Z"), -0.064)
          (DateTimeOffset.Parse("2023-09-23T12:00:00Z"), 1.306)
          (DateTimeOffset.Parse("2023-09-23T13:00:00Z"), 0.206)
          (DateTimeOffset.Parse("2023-09-23T14:00:00Z"), 0.414)
          (DateTimeOffset.Parse("2023-09-23T15:00:00Z"), 0.527)
          (DateTimeOffset.Parse("2023-09-23T16:00:00Z"), 0.602) ]

    // can't compare the lists directly due to floats, so compare each item with tolerance on floats
    for (expected, actual) in List.zip expected result do
        Assert.Equal(fst expected, fst actual)
        Assert.InRange(snd actual, snd expected - TOLERANCE, snd expected + TOLERANCE)
