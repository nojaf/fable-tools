module FableTools.App

open System
open System.IO
open AvaloniaEdit.Editing
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
    registryOptions.GetScopeByLanguageId(registryOptions.GetLanguageByExtension(".fs").Id)

type Model =
    {
        Input : string
        Fsproj : FileInfo
        Configuration : string
        CrackerResult : CrackerResult
        Output : string
    }

type Msg =
    | DocumentChanged of TextInputEventArgs
    | EditorLoaded
    | GetAST
    | ReceiveAST of ast : string
    | CompilerException of exn

let init () =
    let fsproj =
        Path.Combine(__SOURCE_DIRECTORY__, "SampleApp/SampleApp.fsproj") |> FileInfo

    let configuration = "Debug"
    let crackerResult = Service.mkCrackerResponse configuration fsproj

    {
        Input = "let sum a b = a + b"
        Configuration = configuration
        Fsproj = fsproj
        CrackerResult = crackerResult
        Output = ""
    },
    Cmd.none

let update msg model =
    match msg with
    | DocumentChanged args ->
        let nextModel =
            match args.Source with
            | :? TextArea as textArea ->
                { model with
                    Input = textArea.Document.Text
                }
            | _ -> model

        nextModel, Cmd.none
    | EditorLoaded -> model, Cmd.none
    | GetAST ->
        let getAST =
            async {
                try
                    let! file = Service.getAST model.CrackerResult model.Input
                    let expandedAST = ExpandedAST.getExpandedAST file
                    return Msg.ReceiveAST expandedAST
                with ex ->
                    return Msg.CompilerException ex
            }

        model, Cmd.ofAsyncMsg getAST
    | ReceiveAST ast -> { model with Output = ast }, Cmd.none
    | CompilerException exn -> model, Cmd.none

let textEditorOptions =
    let options = TextEditorOptions()
    options.ConvertTabsToSpaces <- true
    options.ShowSpaces <- true
    options.IndentationSize <- 4
    options

let editorLoaded (configureTextEditor : TextEditor -> unit) (ev : Avalonia.Interactivity.RoutedEventArgs) =
    match ev.Source with
    | :? TextEditor as textEditor ->
        configureTextEditor textEditor
        let textMateInstallation = textEditor.InstallTextMate registryOptions
        textMateInstallation.SetGrammar fsharpGrammar
        EditorLoaded
    | _ -> EditorLoaded

let view (model : Model) : WidgetBuilder<Msg, IFabGrid> =
    Grid(coldefs = [ Dimension.Stars 4. ; Pixel 1. ; Dimension.Stars 4. ; Dimension.Stars 2. ], rowdefs = [ Star ]) {
        TextEditor(textEditorOptions)
            .fontFamily("JetBrains Mono, Cascadia Code,Consolas,Menlo,Monospace")
            .showLineNumber(true)
            .fontSize(14.)
            .onTextChanged(DocumentChanged)
            .onLoaded(
                // See: https://github.com/AvaloniaUI/AvaloniaEdit/issues/387
                editorLoaded(fun textEditor -> textEditor.Text <- model.Input)
            )
            .padding(left = 10., top = 0., right = 0., bottom = 0.)

        GridSplitter(GridResizeDirection.Columns)
            .background(SolidColorBrush Colors.Cornsilk)
            .gridColumn(1)

        TextEditor()
            .isReadOnly(true)
            .fontFamily("JetBrains Mono, Cascadia Code,Consolas,Menlo,Monospace")
            .showLineNumber(true)
            .fontSize(14.)
            .onLoaded(editorLoaded ignore)
            .text(model.Output)
            .gridColumn(2)

        Grid(coldefs = [ Dimension.Star ], rowdefs = [ Dimension.Auto ]) { Button("Get AST", GetAST) }
        |> _.gridColumn(4)
    }


let app model = DesktopApplication(Window(view model))

let theme () =
    StyleInclude(baseUri = null, Source = Uri "avares://FableTools/App.xaml")

let program = Program.statefulWithCmd init update app
