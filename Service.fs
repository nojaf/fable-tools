namespace FableTools

open System.Threading
open System.IO
open FSharp.Compiler.SourceCodeServices
open Fable.Compiler
open Fable.Compiler.Util
open Fable.Compiler.ProjectCracker

type CrackerResult =
    {
        Checker : InteractiveChecker
        CliArgs : CliArgs
        CrackerResponse : CrackerResponse
    }

module Service =
    let resolver : ProjectCrackerResolver = MSBuildCrackerResolver()

    let mkCrackerResponse (configuration : string) (fsproj : FileInfo) : CrackerResult =
        let cliArgs : CliArgs =
            {
                ProjectFile = fsproj.FullName
                RootDir = __SOURCE_DIRECTORY__
                OutDir = None
                IsWatch = false
                Precompile = false
                PrecompiledLib = None
                PrintAst = false
                // I don't think this actually has to exists on disk for this tool.
                FableLibraryPath = Some(Path.Combine(fsproj.DirectoryName, "node_modules/@fable-org/fable-library-js"))
                Configuration = configuration
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

        let crackerOptions : CrackerOptions = CrackerOptions(cliArgs, true)
        let crackerResponse : CrackerResponse = getFullProjectOpts resolver crackerOptions
        let checker = InteractiveChecker.Create crackerResponse.ProjectOptions

        {
            Checker = checker
            CliArgs = cliArgs
            CrackerResponse = crackerResponse
        }

    let versionGenerator =
        let counter = ref 0
        (fun () -> Interlocked.Increment(counter))

    let getAST (crackerResult : CrackerResult) sourceCode =
        let lastFile =
            crackerResult.CrackerResponse.ProjectOptions.SourceFiles |> Array.last

        let sourceReader _ = versionGenerator(), (lazy sourceCode)

        CodeServices.compileFileToFableAST
            sourceReader
            crackerResult.Checker
            crackerResult.CliArgs
            crackerResult.CrackerResponse
            lastFile
