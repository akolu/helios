module Helios.Core.Logger

open Newtonsoft.Json
open System.Net.Http

type ILogger =
    abstract member LogJson: 'T -> unit

type ConsoleLogger() =
    interface ILogger with
        member _.LogJson(data: 'T) =
            printfn "%s" (JsonConvert.SerializeObject(data, Formatting.Indented))
