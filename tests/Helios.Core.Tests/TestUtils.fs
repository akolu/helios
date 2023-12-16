module TestUtils

open Helios.Core.Logger

type MockLogger() =
    interface ILogger with
        member _.LogJson(data: Result<'T, string>) = ()
