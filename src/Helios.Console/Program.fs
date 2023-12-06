open Helios.Core.Main
open Helios.Console.ConsoleUI.Commands
open Helios.Console.ConsoleUI.UI
open Spectre.Console

type State = { App: App }

let rec mainLoop state =
    match mainPrompt () with
    | Import ->
        match importPrompt () with
        | FusionSolar -> state.App |> importFusionSolar (askDate "Date: ")

        mainLoop (state)
    | Quit -> ()

    let confirm = AnsiConsole.Confirm("Are you sure you want to quit?")
    if confirm then () else mainLoop (state)

[<EntryPoint>]
let main argv =
    figlet ()

    match argv with
    | [| dbPath |] ->
        let helios = App.Init(dbPath)
        mainLoop ({ App = helios })
        0
    | _ ->
        (printfn "Usage: dotnet run <dbPath>")
        1
