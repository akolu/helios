module Helios.Core.Services.FusionSolar

open Helios.Core.Utils
open System.Net.Http
open Helios.Core.Logger

module Constants =
    let XSRF_TOKEN_COOKIE_KEY = "XSRF-TOKEN"

module EndpointUrls =
    let baseUrl = "https://eu5.fusionsolar.huawei.com/thirdData"
    let login = baseUrl + "/login"
    let getStations = baseUrl + "/stations"
    let getHourlyData = baseUrl + "/getKpiStationHour"

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
        type DataPoint =
            { stationCode: string
              collectTime: int64
              dataItemMap:
                  {| radiation_intensity: int64
                     inverter_power: int64
                     power_profit: int64
                     theory_power: int64
                     ongrid_power: int64 |} }

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
    { httpClient: IHttpHandler
      logger: ILogger
      userName: string
      systemCode: string }

type FusionSolar = { isLoggedIn: bool; config: Config }

let init (config: Config) = { isLoggedIn = false; config = config }

let private login (this: FusionSolar) =
    match
        this.config.httpClient.Post(
            EndpointUrls.login,
            (HttpUtils.toJsonStringContent
                {| userName = this.config.userName
                   systemCode = this.config.systemCode |})
        )
        |> tap (fun response ->
            response
            >>= HttpUtils.parseJsonBody<Types.Login.ResponseBody>
            |> this.config.logger.LogJson)
        >>= HttpUtils.parseCookie Constants.XSRF_TOKEN_COOKIE_KEY
    with
    | Ok xsrfToken ->
        this.config.httpClient.SetCookie(EndpointUrls.baseUrl, Constants.XSRF_TOKEN_COOKIE_KEY, xsrfToken) // mutate :(
        Ok { this with isLoggedIn = true }
    | Error err -> Error err


let rec getStations (this: FusionSolar) =
    match this.isLoggedIn with
    | false -> login this >>= getStations
    | true ->
        this.config.httpClient.Post(EndpointUrls.getStations, (HttpUtils.toJsonStringContent {| pageNo = 1 |}))
        >>= HttpUtils.parseJsonBody<Types.GetStations.ResponseBody>
        |> tap this.config.logger.LogJson


let rec getHourlyData (stationCode: Types.GetHourlyData.RequestBody) (this: FusionSolar) =
    match this.isLoggedIn with
    | false -> login this >>= (getHourlyData stationCode)
    | true ->
        this.config.httpClient.Post(EndpointUrls.getHourlyData, (HttpUtils.toJsonStringContent stationCode))
        >>= HttpUtils.parseJsonBody<Types.GetHourlyData.ResponseBody>
        |> tap this.config.logger.LogJson
