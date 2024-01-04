open Helios.Core.Main
open Helios.Console.ConsoleUI.Commands
open Helios.Console.ConsoleUI.UI
open Spectre.Console
open System.Threading
open Microsoft.Extensions.Logging

type State = { App: App }

let LOG_LEVEL = LogLevel.Information

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
                                Thread.Sleep(1 * 60 * 1000)
                )

        | EntsoE -> state.App.Services.EntsoE.Import(askDate "Date from: ", askDate "Date to: ")
        | Fingrid ->
            match csvPrompt with
            | None -> AnsiConsole.MarkupLine("[red]No CSV files found in the current directory[/]")
            | Some csvPath -> state.App.Services.Fingrid.Import csvPath |> ignore

        mainLoop (state)
    | GenerateReport ->
        state.App.Services.Reporting.EnergySavings()
        |> reportTable (
            [| "Time"
               "Consumption"
               "Production"
               "Surplus"
               "Spot price"
               "Savings"
               "Savings acc"
               "Sold to grid" |],
            (fun row ->
                [ row.Time.LocalDateTime.ToString()
                  row.Consumption.ToString("0.00")
                  row.Production.ToString("0.00")
                  row.Surplus.ToString("0.00")
                  row.SpotPrice.ToString("0.00")
                  row.Savings.ToString("0.00")
                  row.SavingsAcc.ToString("0.00")
                  row.SoldToGrid.ToString("0.00") ])
        )

        mainLoop (state)
    | Quit ->
        let confirm = AnsiConsole.Confirm("Are you sure you want to quit?")
        if confirm then () else mainLoop (state)

[<EntryPoint>]
let main argv =
    figlet ()

    let helios =
        match argv with
        | [| dbPath |] -> App.Init(LOG_LEVEL, dbPath)
        | _ -> App.Init(LOG_LEVEL)

    mainLoop ({ App = helios })
    0
