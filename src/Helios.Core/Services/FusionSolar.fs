module Helios.Core.Services.FusionSolar

open Helios.Utils
open System.Net.Http

module Constants =
    let XSRF_TOKEN_COOKIE_KEY = "XSRF-TOKEN"

module EndpointUrls =
    let baseUrl = "https://eu5.fusionsolar.huawei.com/thirdData"
    let login = baseUrl + "/login"
    let getStations = baseUrl + "/stations"
    let getHourlyData = baseUrl + "/getKpiStationHour"

module Types =
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

        type GetStationsResponse =
            { success: bool
              failCode: int
              message: string option
              data: Data }

type Config =
    { httpClient: IHttpHandler
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
        |> HttpUtils.parseCookie Constants.XSRF_TOKEN_COOKIE_KEY
    with
    | Some xsrfToken ->
        this.config.httpClient.SetCookie(EndpointUrls.baseUrl, Constants.XSRF_TOKEN_COOKIE_KEY, xsrfToken) // mutate :(
        Ok { this with isLoggedIn = true }
    | None -> Error(HttpRequestException "Could not log in")

let rec getStations (this: FusionSolar) =
    match this.isLoggedIn with
    | false -> login this >>= getStations
    | true ->
        this.config.httpClient.Post(EndpointUrls.getStations, (HttpUtils.toJsonStringContent {| pageNo = 1 |}))
        |> HttpUtils.parseJsonBody<Types.GetStations.GetStationsResponse>

let rec getKpiStationHour (this: FusionSolar) =
    match this.isLoggedIn with
    | false -> login this >>= getKpiStationHour
    | true -> Ok "getStationRealKpi stub"
