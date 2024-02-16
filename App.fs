module FableTools.App

open System
open TextMateSharp.Grammars
open Avalonia.Controls
open Avalonia.Input
open Avalonia.Markup.Xaml.Styling
open Avalonia.Media
open AvaloniaEdit
open AvaloniaEdit.TextMate
open Fabulous
open Fabulous.Avalonia
open type Fabulous.Avalonia.View

let registryOptions = RegistryOptions ThemeName.LightPlus

let fsharpGrammar =
    registryOptions.GetScopeByLanguageId (registryOptions.GetLanguageByExtension(".fs").Id)

type Model = { Input : string }

type Msg =
    | DocumentChanged of TextInputEventArgs
    | EditorLoaded

let init () =
    {
        Input =
            """
let update msg model =
    match msg with
    | DocumentChanged args -> { Document = args.NewDocument }, Cmd.none
    | EditorLoaded -> model, Cmd.none"""
    },
    Cmd.none

let update msg model =
    match msg with
    | DocumentChanged args -> { Input = args.Text }, Cmd.none
    | EditorLoaded -> model, Cmd.none

let textEditorOptions =
    let options = TextEditorOptions ()
    options.ConvertTabsToSpaces <- true
    options.ShowSpaces <- true
    options.IndentationSize <- 4
    options

let view (model : Model) : WidgetBuilder<Msg, IFabGrid> =
    Grid (coldefs = [ Star ; Auto ], rowdefs = []) {
        Grid (coldefs = [ Dimension.Stars 4. ; Pixel 1. ; Dimension.Stars 4. ; Dimension.Stars 2. ], rowdefs = [ Star ]) {
            TextEditor(DocumentChanged, textEditorOptions)
                .fontFamily("JetBrains Mono, Cascadia Code,Consolas,Menlo,Monospace")
                .showLineNumber(true)
                .fontSize(14.)
                .onLoaded (fun ev ->
                    match ev.Source with
                    | :? TextEditor as textEditor ->
                        // See: https://github.com/AvaloniaUI/AvaloniaEdit/issues/387
                        textEditor.Text <- model.Input
                        let textMateInstallation = textEditor.InstallTextMate registryOptions
                        textMateInstallation.SetGrammar fsharpGrammar
                        EditorLoaded
                    | _ -> EditorLoaded
                )

            GridSplitter(GridResizeDirection.Columns)
                .background(SolidColorBrush Colors.Cornsilk)
                .gridColumn(1)

            Rectangle().fill(SolidColorBrush Colors.CornflowerBlue).gridColumn 2
            Rectangle().fill(SolidColorBrush Colors.Green).gridColumn 3
        }
    }

let app model =
    DesktopApplication (Window (view model))

let theme () =
    StyleInclude (baseUri = null, Source = Uri "avares://FableTools/App.xaml")

let program = Program.statefulWithCmd init update app
