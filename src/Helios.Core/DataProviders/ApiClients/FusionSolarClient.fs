module Helios.Core.DataProviders.ApiClients.FusionSolarClient

open Helios.Core.Utils
open Helios.Core.HttpHandler
open Microsoft.Extensions.Logging

module Constants =
    let XSRF_TOKEN_COOKIE_KEY = "XSRF-TOKEN"

module EndpointUrls =
    let BaseUrl = "https://eu5.fusionsolar.huawei.com/thirdData"
    let Login = BaseUrl + "/login"
    let GetStations = BaseUrl + "/stations"
    let GetHourlyData = BaseUrl + "/getKpiStationHour"

module Types =
    module Login =
        type ResponseBody =
            { success: bool
              failCode: int
              ``params``: {| currentTime: int64 |}
              message: string option }

    module GetStations =
        type PlantInfo =
            { plantCode: string
              plantName: string
              plantAddress: string
              longitude: double
              latitude: double
              capacity: double
              contactPerson: string
              contactMethod: string
              gridConnectionDate: string }

        type Data =
            { total: int64
              pageCount: int64
              pageNo: int
              pageSize: int
              list: PlantInfo list }

        type ResponseBody =
            { success: bool
              failCode: int
              message: string option
              data: Data }

    module GetHourlyData =
        type DataItemMap =
            { radiation_intensity: double option
              inverter_power: double option
              power_profit: double option
              theory_power: double option
              ongrid_power: double option }

        type DataPoint =
            { stationCode: string
              collectTime: int64
              dataItemMap: DataItemMap }

        type ResponseBody =
            { success: bool
              failCode: int
              ``params``:
                  {| stationCodes: string
                     currentTime: int64
                     collectTime: int64 |}
              message: string option
              data: DataPoint list }

        type RequestBody =
            { stationCodes: string
              collectTime: int64 }

type Config =
    { HttpClient: IHttpHandler
      Logger: ILogger
      UserName: string
      SystemCode: string }

type FusionSolar =
    { IsLoggedIn: bool
      Config: Config }

    static member Init(config: Config) = { IsLoggedIn = false; Config = config }

let private login (this: FusionSolar) =
    match
        this.Config.HttpClient.Post(
            EndpointUrls.Login,
            (HttpUtils.toJsonStringContent
                {| userName = this.Config.UserName
                   systemCode = this.Config.SystemCode |})
        )
        |> tap (fun response ->
            response
            >>= HttpUtils.parseJsonBody<Types.Login.ResponseBody>
            |> (jsonResultToString >> this.Config.Logger.LogDebug))
        >>= HttpUtils.parseCookie Constants.XSRF_TOKEN_COOKIE_KEY
    with
    | Ok xsrfToken ->
        this.Config.HttpClient.SetAuthHeader(Constants.XSRF_TOKEN_COOKIE_KEY, xsrfToken)
        Ok { this with IsLoggedIn = true }
    | Error err -> Error err


let rec getStations (this: FusionSolar) =
    match this.IsLoggedIn with
    | false -> login this >>= getStations
    | true ->
        this.Config.HttpClient.Post(EndpointUrls.GetStations, (HttpUtils.toJsonStringContent {| pageNo = 1 |}))
        >>= HttpUtils.parseJsonBody<Types.GetStations.ResponseBody>
        |> tap (jsonResultToString >> this.Config.Logger.LogDebug)


let rec getHourlyData (body: Types.GetHourlyData.RequestBody) (this: FusionSolar) =
    match this.IsLoggedIn with
    | false -> login this >>= (getHourlyData body)
    | true ->
        this.Config.HttpClient.Post(EndpointUrls.GetHourlyData, (HttpUtils.toJsonStringContent body))
        >>= HttpUtils.parseJsonBody<Types.GetHourlyData.ResponseBody>
        |> tap (jsonResultToString >> this.Config.Logger.LogDebug)
