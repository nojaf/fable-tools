open System
open System.IO
open FSharp.Compiler.SourceCodeServices
open Fable.Compiler
open Fable.Compiler.Util
open Fable.Compiler.ProjectCracker
open Avalonia
open Fabulous.Avalonia

let fsproj =
    @"C:\Users\nojaf\Projects\vite-plugin-fable\sample-project\App.fsproj"
    |> FileInfo

let cliArgs : CliArgs =
    {
        ProjectFile = fsproj.FullName
        RootDir = __SOURCE_DIRECTORY__
        OutDir = None
        IsWatch = false
        Precompile = false
        PrecompiledLib = None
        PrintAst = false
        FableLibraryPath = Some (Path.Combine (fsproj.DirectoryName, "node_modules/fable-library"))
        Configuration = "Debug"
        NoRestore = false
        NoCache = true
        NoParallelTypeCheck = false
        SourceMaps = false
        SourceMapsRoot = None
        Exclude = []
        Replace = Map.empty
        RunProcess = None
        CompilerOptions =
            {
                TypedArrays = true
                ClampByteArrays = false
                Language = Fable.Language.JavaScript
                Define = [ "FABLE_COMPILER" ; "FABLE_COMPILER_4" ; "FABLE_COMPILER_JAVASCRIPT" ]
                DebugMode = true
                OptimizeFSharpAst = false
                Verbosity = Fable.Verbosity.Verbose
                FileExtension = ".fs"
                TriggeredByDependency = false
                NoReflection = false
            }
        Verbosity = Fable.Verbosity.Verbose
    }

let crackerOptions : CrackerOptions = CrackerOptions (cliArgs, true)
let resolver : ProjectCrackerResolver = MSBuildCrackerResolver ()
let crackerResponse = getFullProjectOpts resolver crackerOptions
let checker = InteractiveChecker.Create crackerResponse.ProjectOptions

let sourceReader =
    Fable.Compiler.File.MakeSourceReader (Array.map Fable.Compiler.File crackerResponse.ProjectOptions.SourceFiles)
    |> snd

let lastFile = crackerResponse.ProjectOptions.SourceFiles |> Array.last

let ast =
    CodeServices.compileFileToFableAST sourceReader checker cliArgs crackerResponse lastFile
    |> Async.RunSynchronously

let expandedAST = FableTools.ExpandedAST.getExpandedAST ast

printfn "%s" expandedAST

open System
open Avalonia
open Fabulous.Avalonia
open FableTools

// Initialization code. Don't use any Avalonia, third-party APIs or any
// SynchronizationContext-reliant code before AppMain is called: things aren't initialized
// yet and stuff might break.
[<STAThread; EntryPoint>]
let Main (args: string array) =
    AppBuilder
        .Configure(fun () ->
            let app = Program.startApplication App.program
            app.Styles.Add(App.theme ())
            app)
        .UsePlatformDetect()
        .LogToTrace(?level = None)
        .StartWithClassicDesktopLifetime(args)
