module Helios.Core.Services.FusionSolar

open Helios.Utils

module private Constants =
    let XSRF_TOKEN_COOKIE_KEY = "XSRF-TOKEN"

module private EndpointUrls =
    let baseUrl = "https://eu5.fusionsolar.huawei.com/thirdData"
    let login = baseUrl + "/login"

type Config =
    { httpClient: IHttpHandler
      userName: string
      systemCode: string }

type FusionSolar = { xsrfToken: string; config: Config }

let init (config: Config) = { xsrfToken = ""; config = config }

let login (this: FusionSolar) =
    match
        this.config.httpClient.Post(
            EndpointUrls.login,
            (HttpUtils.toJsonStringContent
                {| userName = this.config.userName
                   systemCode = this.config.systemCode |})
        )
        |> HttpUtils.parseCookie Constants.XSRF_TOKEN_COOKIE_KEY
    with
    | Some xsrfToken -> Ok { this with xsrfToken = xsrfToken }
    | None -> Error "Could not log in"

let rec getStations (this: FusionSolar) =
    match this.xsrfToken with
    | "" -> login this >>= getStations
    | _ -> Ok "getStations stub"

let rec getKpiStationHour (this: FusionSolar) =
    match this.xsrfToken with
    | "" -> login this >>= getKpiStationHour
    | _ -> Ok "getStationRealKpi stub"
