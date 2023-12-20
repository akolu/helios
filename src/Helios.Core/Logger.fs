module Helios.Core.Logger

open Microsoft.Extensions.Logging
open System

type NoOpDisposable() =
    interface IDisposable with
        member _.Dispose() = ()

type LoggerOptions =
    { Print: string -> unit
      Level: LogLevel }

    static member Default =
        { Print = (printfn "%s")
          Level = LogLevel.Information }

    static member None =
        { Print = ignore
          Level = LogLevel.None }

type HeliosLoggerProvider(?opts: LoggerOptions) =
    interface ILoggerProvider with

        member _.CreateLogger(categoryName) =
            let options = defaultArg opts LoggerOptions.Default

            { new ILogger with
                member _.BeginScope<'TState>(state: 'TState) = new NoOpDisposable() :> IDisposable

                member _.IsEnabled(logLevel) = logLevel >= options.Level

                member _.Log<'TState>(logLevel, eventId, state: 'TState, ex, formatter) =
                    options.Print(formatter.Invoke(state, ex)) }

        member _.Dispose() = ()

let createLogger (provider: ILoggerProvider) =
    LoggerFactory
        .Create(fun builder ->
            builder
                .AddFilter(fun (category: string) (level: LogLevel) -> level >= LogLevel.Debug)
                .AddProvider(provider)
            |> ignore)
        .CreateLogger("HeliosLogger")
