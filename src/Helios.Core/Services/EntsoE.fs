module Helios.Core.Services.EntsoE

open Helios.Core.Utils
open Helios.Core.Logger
open System
open System.Xml.Linq

module Constants =
    let API_URL = "https://web-api.tp.entsoe.eu/api"
    let DOCUMENT_TYPE_TRANSMISSION_DAY_AHEAD_PRICES = "A44"
    let AREA_CODE_FINLAND = "10YFI-1--------U"

module Types =

    type TimeSeriesPeriodInterval =
        { startDt: DateTimeOffset
          endDt: DateTimeOffset }

    type TimeSeriesPoint = { position: int; priceAmount: double }

    type TimeSeriesPeriod =
        { timeInterval: TimeSeriesPeriodInterval
          points: TimeSeriesPoint list }

type Config =
    { httpClient: IHttpHandler
      logger: ILogger
      securityToken: string }

type EntsoE = { config: Config }

let init (config: Config) = { config = config }

let private parseTimeInterval (ns: XNamespace) (element: XElement) =
    match Seq.tryHead (element.Descendants(ns + "timeInterval")) with
    | None -> Error "TimeInterval element not found"
    | Some interval ->
        Ok
            { Types.TimeSeriesPeriodInterval.startDt = DateTimeOffset.Parse(interval.Element(ns + "start").Value)
              Types.TimeSeriesPeriodInterval.endDt = DateTimeOffset.Parse(interval.Element(ns + "end").Value) }

let private parsePoints (ns: XNamespace) (element: XElement) =
    element.Descendants(ns + "Point")
    |> Seq.map (fun p ->
        { Types.TimeSeriesPoint.position = int (p.Element(ns + "position").Value)
          Types.TimeSeriesPoint.priceAmount = double (p.Element(ns + "price.amount").Value) })
    |> Seq.toList

let private parsePeriod (ns: XNamespace) (period: XElement) =
    parseTimeInterval ns period
    |> Result.map (fun timeInterval ->
        { Types.TimeSeriesPeriod.timeInterval = timeInterval
          Types.TimeSeriesPeriod.points = parsePoints ns period })

let private parseTimeSeries (ns: XNamespace) (element: XElement) =
    match Seq.tryHead (element.Descendants(ns + "Period")) with
    | None -> Error "Period element not found"
    | Some period -> parsePeriod ns period

let private toTimeSeriesPeriodList (doc: XDocument) =
    let ns = doc.Root.GetDefaultNamespace()

    doc.Root.Descendants(ns + "TimeSeries")
    |> Seq.toList
    |> traverse (parseTimeSeries ns)

let getDayAheadPrices (dateFrom: DateTimeOffset, dateTo: DateTimeOffset) (this: EntsoE) =
    this.config.httpClient.Get(
        HttpUtils.buildUrlWithParams
            Constants.API_URL
            (Map.ofList
                [ "documentType", Constants.DOCUMENT_TYPE_TRANSMISSION_DAY_AHEAD_PRICES
                  "in_Domain", Constants.AREA_CODE_FINLAND
                  "out_Domain", Constants.AREA_CODE_FINLAND
                  "timeInterval", dateTimeToUTCISOString dateFrom + "/" + dateTimeToUTCISOString dateTo
                  "securityToken", this.config.securityToken ])
    )
    >>= HttpUtils.parseXmlBody
    >>= toTimeSeriesPeriodList
