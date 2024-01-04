namespace Helios.Console.ConsoleUI

open Spectre.Console
open Spectre.Console.Rendering
open Helios.Core.Utils
open System


module Commands =
    type Main =
        | Import
        | GenerateReport
        | Quit

    type Import =
        | FusionSolar
        | EntsoE
        | Fingrid

    let mainPrompt () =
        AnsiConsole.WriteLine()

        SelectionPrompt<Main>()
        |> tap (fun p -> p.Title <- "Please choose a command: ")
        |> fun p -> p.AddChoices [ Import; GenerateReport; Quit ]
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

    let reportTable (columns: string[], render: 'T -> string list) (report: 'T list) =
        Table().AddColumns(columns)
        |> tap (fun t ->
            // apply table styles
            t.Border <- TableBorder.SimpleHeavy
            t.Columns.[0].Padding <- new Padding(4, 0)

            report
            |> List.iteri (fun i row ->
                let styled (s: string) : IRenderable =
                    Markup(if i % 2 = 0 then "[grey66]" + s + "[/]" else s)
                // call render function, apply styles to every other row, convert to array
                t.AddRow(render row |> List.map (fun txt -> styled txt) |> Array.ofList)
                |> ignore))
        |> AnsiConsole.Write
