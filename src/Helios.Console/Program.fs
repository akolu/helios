open Helios.Core.Main
open Helios.Console.ConsoleUI.Commands
open Helios.Console.ConsoleUI.UI
open Spectre.Console
open System.Threading

type State = { App: App }

let rec mainLoop state =
    match mainPrompt () with
    | Import ->
        match importPrompt () with
        | FusionSolar ->
            let dateFrom = askDate "Date from: "
            let dateTo = askDate "Date to: "
            let diff = (dateTo - dateFrom).Days

            AnsiConsole
                .Status()
                .Start(
                    (sprintf "Importing FusionSolar: %O (1/%d)" dateFrom (diff + 1)),
                    fun ctx ->
                        ctx.Spinner <- Spinner.Known.Dots
                        ctx.SpinnerStyle <- Style.Parse("green")

                        for i in 0..diff do
                            let date = dateFrom.AddDays(float i)
                            state.App.Services.FusionSolar.Import date
                            ctx.Status <- (sprintf "Importing FusionSolar: %O (%d/%d)" date (i + 1) (diff + 1))
                            ctx.Refresh()

                            if i < diff then
                                Thread.Sleep(10 * 60 * 1000)
                )

        | EntsoE -> state.App.Services.EntsoE.Import(askDate "Date from: ", askDate "Date to: ")
        | Fingrid ->
            match csvPrompt with
            | None -> AnsiConsole.MarkupLine("[red]No CSV files found in the current directory[/]")
            | Some csvPath -> state.App.Services.Fingrid.Import csvPath |> ignore

        mainLoop (state)
    | Export -> state.App.Services.Reporting.generateReport |> ignore
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
