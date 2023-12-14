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

let takeLast num (xs: 'a list) =
    xs |> List.rev |> List.take (Math.Min(xs.Length, num)) |> List.rev

let dateTimeToUTCISOString (date: DateTimeOffset) = date.ToUniversalTime().ToString("o")
