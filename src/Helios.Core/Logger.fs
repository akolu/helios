module Helios.Core.Logger

type ILogger =
    abstract member LogJson: Result<'T, string> -> unit

type ConsoleLogger() =
    interface ILogger with
        member _.LogJson(data) =
            let options = System.Text.Json.JsonSerializerOptions()
            options.WriteIndented <- true

            match data with
            | Ok json -> printfn "%s" (System.Text.Json.JsonSerializer.Serialize(json, options))
            | Error err -> printfn "Error: %s" err
