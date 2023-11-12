module Helios.Utils

open System

let (>>=) x f = Result.bind f x

let unwrap res =
    match res with
    | Ok res -> res
    | Error err -> raise (Exception(err.ToString()))
