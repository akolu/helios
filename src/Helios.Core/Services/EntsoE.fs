module Helios.Core.Services.EntsoE

open Helios.Core.Utils
open Helios.Core.Logger
open System

module Constants =
    let DOCUMENT_TYPE_TRANSMISSION_DAY_AHEAD_PRICES = "A44"
    let AREA_CODE_FINLAND = "10YFI-1--------U"

module EndpointUrls =
    let apiUrl = "https://web-api.tp.entsoe.eu/api"

type Config =
    { httpClient: IHttpHandler
      logger: ILogger
      securityToken: string }

type EntsoE = { config: Config }

let init (config: Config) = { config = config }

let getDayAheadPrices (dateFrom: DateTimeOffset, dateTo: DateTimeOffset) (this: EntsoE) =
    this.config.httpClient.Get(
        HttpUtils.buildUrlWithParams
            EndpointUrls.apiUrl
            (Map.ofList
                [ "documentType", Constants.DOCUMENT_TYPE_TRANSMISSION_DAY_AHEAD_PRICES
                  "in_Domain", Constants.AREA_CODE_FINLAND
                  "out_Domain", Constants.AREA_CODE_FINLAND
                  "timeInterval", dateTimeToUTCISOString dateFrom + "/" + dateTimeToUTCISOString dateTo
                  "securityToken", this.config.securityToken ])
    )
    >>= HttpUtils.parseXmlBody
    |> tap (fun result ->
        match result with
        | Ok doc -> printfn "%s" (doc.ToString())
        | Error err -> printfn "%s" (err.ToString()))
