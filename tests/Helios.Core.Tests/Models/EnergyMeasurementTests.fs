module EnergyMeasurementTests

open Xunit
open System
open Helios.Core.Models.EnergyMeasurement
open Helios.Core.Services.FusionSolar

[<Fact>]
let ``fromFusionSolarResponse should map response to EnergyMeasurement list`` () =
    let time = DateTimeOffset.Parse("2021-01-01T00:00:00+00:00")
    let unixTime = time.ToUnixTimeMilliseconds()

    let responseBody: Types.GetHourlyData.ResponseBody =
        { failCode = 0
          message = None
          success = true
          ``params`` =
            {| stationCodes = "123"
               currentTime = unixTime
               collectTime = unixTime |}
          data =
            [ { stationCode = "123"
                collectTime = unixTime
                dataItemMap =
                  { radiation_intensity = Some 1
                    inverter_power = Some 0.16
                    power_profit = Some 1
                    theory_power = None
                    ongrid_power = None } } ] }

    let expected = [ new EnergyMeasurement(time, FlowType.Production, 0.16) ]
    let actual = EnergyMeasurement.fromFusionSolarResponse responseBody
    Assert.Equal<EnergyMeasurement list>(expected, actual)
