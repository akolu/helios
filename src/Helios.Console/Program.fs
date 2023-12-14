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
        | EntsoE -> state.App |> importEntsoE (askDate "Date from: ", askDate "Date to: ")

        mainLoop (state)
    | Quit ->
        let confirm = AnsiConsole.Confirm("Are you sure you want to quit?")
        if confirm then () else mainLoop (state)

[<EntryPoint>]
let main argv =
    figlet ()

    let helios =
        match argv with
        | [| dbPath |] -> App.Init(dbPath)
        | _ -> App.Init()

    mainLoop ({ App = helios })
    0
