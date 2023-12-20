module Helios.Core.Utils

open System

let (>>=) x f = Result.bind f x

let unwrap res =
    match res with
    | Ok res -> res
    | Error err -> raise (Exception(err.ToString()))

let tap f x =
    f x
    x

let rec traverse f =
    function
    | [] -> Ok []
    | x :: xs ->
        match f x with
        | Error e -> Error e
        | Ok y ->
            match traverse f xs with
            | Error e -> Error e
            | Ok ys -> Ok(y :: ys)

let takeLast num (xs: 'a list) =
    xs |> List.rev |> List.take (Math.Min(xs.Length, num)) |> List.rev

let dateTimeToUTCISOString (date: DateTimeOffset) = date.ToUniversalTime().ToString("o")

let jsonResultToString data =
    let options = Text.Json.JsonSerializerOptions()
    options.WriteIndented <- true

    match data with
    | Ok json -> System.Text.Json.JsonSerializer.Serialize(json, options)
    | Error err -> err
