namespace Helios.Console.ConsoleUI

open Spectre.Console
open Helios.Core.Utils
open System

module Commands =
    type Main =
        | Import
        | Quit

    type Import =
        | FusionSolar
        | EntsoE
        | Fingrid

    let mainPrompt () =
        AnsiConsole.WriteLine()

        SelectionPrompt<Main>()
        |> tap (fun p -> p.Title <- "Please choose a command: ")
        |> fun p -> p.AddChoices [ Import; Quit ]
        |> AnsiConsole.Prompt

    let importPrompt () =
        SelectionPrompt<Import>()
        |> tap (fun p -> p.Title <- "Please select one or more items: ")
        |> fun p -> p.AddChoices [ FusionSolar; EntsoE; Fingrid ]
        |> AnsiConsole.Prompt

    let rec askDate text =
        let prompt = AnsiConsole.Ask<string>(text)
        let success, date = DateTimeOffset.TryParse(prompt)

        if success then
            date
        else
            AnsiConsole.MarkupLine("[red]Invalid Date. Input date in dd.MM.yyyy format[/]")
            askDate text

    let private listCsvFilesWithExtension =
        let exePath = Reflection.Assembly.GetExecutingAssembly().Location
        let directory = System.IO.Path.GetDirectoryName(exePath)
        let files = System.IO.Directory.GetFiles(directory, "*.csv")
        files |> List.ofArray

    let csvPrompt =
        match listCsvFilesWithExtension with
        | [] -> None
        | files ->
            Some(
                SelectionPrompt<string>()
                |> tap (fun p -> p.Title <- "Please select a CSV file: ")
                |> fun p -> p.AddChoices files
                |> AnsiConsole.Prompt
            )

module UI =
    let figlet () =
        AnsiConsole.WriteLine()
        AnsiConsole.WriteLine()

        FigletText(FigletFont.Load("kban.flf"), "Helios") |> AnsiConsole.Write

    let serviceTable () =
        Table()
        |> tap (fun t ->
            t.AddColumns
                [| TableColumn("Service name")
                   TableColumn("Row count")
                   TableColumn("Latest event") |]
            |> ignore)
        |> tap (fun t -> t.Border <- TableBorder.Horizontal)
        |> tap (fun t ->
            t
                .AddRow("FusionSolar", "foo")
                .AddRow("ENTSO-E", "bar")
                .AddRow("Fingrid Datahub", "baz")
            |> ignore)
        |> AnsiConsole.Write
        |> AnsiConsole.WriteLine

    let eventLog (events: (DateTime * string) list) =
        events
        |> takeLast 5
        |> List.map (fun (k, v) -> "[blue]" + k.ToString("HH\:mm\:ss") + "[/] ~ " + v)
        |> List.iter AnsiConsole.MarkupLine

        AnsiConsole.WriteLine()
