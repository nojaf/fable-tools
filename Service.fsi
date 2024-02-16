namespace FableTools

open System.IO
open FSharp.Compiler.SourceCodeServices
open Fable.Compiler.Util
open Fable.Compiler.ProjectCracker

type CrackerResult =
    {
        Checker : InteractiveChecker
        CliArgs : CliArgs
        CrackerResponse : CrackerResponse
    }

module Service =
    val mkCrackerResponse : configuration : string -> fsproj : FileInfo -> CrackerResult
    val getAST : crackerResult : CrackerResult -> sourceCode : string -> Async<Fable.AST.Fable.File>
