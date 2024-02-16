module FableTools.ExpandedAST

open System
open System.Collections.Generic
open FSharp.Reflection
open Fantomas.FCS.Text
open Fantomas.Core
open Fantomas.Core.SyntaxOak

let zeroRange = Range.Zero
let stn v = SingleTextNode(v, zeroRange)

let identList values =
    match values with
    | [] -> IdentListNode([], zeroRange)
    | [ singleValue ] -> IdentListNode([ IdentifierOrDot.Ident(stn singleValue) ], zeroRange)
    | head :: tail ->
        let content =
            [
                yield IdentifierOrDot.Ident(stn head)
                for t in tail do
                    yield IdentifierOrDot.UnknownDot
                    yield IdentifierOrDot.Ident(stn t)
            ]

        IdentListNode(content, zeroRange)

let wrapInQuotes s = String.Concat("\"", s, "\"")

let mkConstExpr v =
    SingleTextNode(v, zeroRange) |> Constant.FromText |> Expr.Constant

let mkStringExpr v = wrapInQuotes v |> mkConstExpr

let mkExprTuple xs =
    match xs with
    | []
    | [ _ ] -> failwith "No point in making a tuple out of less than two items"
    | head :: tail ->
        let content =
            [
                yield Choice1Of2 head
                for t in tail do
                    yield Choice2Of2(stn ",")
                    yield Choice1Of2 t
            ]

        ExprTupleNode(content, zeroRange) |> Expr.Tuple

let mkExprParen e =
    ExprParenNode(stn "(", e, stn ")", zeroRange) |> Expr.Paren

let mkInfix leftExpr operatorName rightExpr =
    ExprInfixAppNode(leftExpr, stn operatorName, rightExpr, zeroRange)
    |> Expr.InfixApp

let mkArrayExpr elements =
    ExprArrayOrListNode(stn "[|", elements, stn "|]", zeroRange) |> Expr.ArrayOrList

let mkListExpr elements =
    ExprArrayOrListNode(stn "[", elements, stn "]", zeroRange) |> Expr.ArrayOrList

let mkExprOptVarNode name =
    ExprOptVarNode(false, name, zeroRange) |> Expr.OptVar

let mkExprIdent v = Expr.Ident(stn v)

let _mkExprAppSingleParenArgNode functionName argExpr =
    ExprAppSingleParenArgNode(mkExprIdent functionName, argExpr, zeroRange)
    |> Expr.AppSingleParenArg

let rec map (value : obj) : Expr =
    if isNull value then
        // Assume `null` is an empty option in our case
        Expr.Ident(stn "None")
    else

    let t = value.GetType()

    if FSharpType.IsUnion t then
        mapUnion value t
    elif FSharpType.IsTuple t then
        FSharpValue.GetTupleFields value
        |> Array.map map
        |> Array.toList
        |> mkExprTuple
        |> mkExprParen
    elif FSharpType.IsRecord t then
        let fieldDefs = FSharpType.GetRecordFields t

        let recordFields =
            fieldDefs
            |> Array.map(fun rf ->
                RecordFieldNode(identList [ rf.Name ], stn "=", map(FSharpValue.GetRecordField(value, rf)), zeroRange)
            )
            |> Array.toList

        ExprRecordNode(stn "{", None, recordFields, stn "}", zeroRange) |> Expr.Record
    elif t.IsEnum then
        $"%s{t.Name}.%A{value}" |> mkConstExpr
    elif t.Name = "FSharpSet`1" then
        ExprAppNode(mkConstExpr "set", [ mapList value ], zeroRange) |> Expr.App
    else

    match value with
    | :? string as s -> wrapInQuotes s |> mkConstExpr
    | :? bool as b -> mkConstExpr(if b then "true" else "false")
    | :? int32 as i -> mkConstExpr(string i)
    | :? IEnumerable<obj> as seqValue -> seqValue |> Seq.toList |> List.map map |> mkListExpr
    | _ -> mkConstExpr $"%A{value}"

and mapUnion (value : obj) (t : System.Type) : Expr =
    let caseInfo, caseFields = FSharpValue.GetUnionFields(value, t)

    if caseInfo.DeclaringType.Name = "FSharpList`1" then
        mapList value
    else

    let caseName =
        identList
            [
                if caseInfo.DeclaringType.Name <> "FSharpOption`1" then
                    yield caseInfo.DeclaringType.Name
                yield caseInfo.Name
            ]

    if caseFields.Length = 0 then
        mkExprOptVarNode caseName
    else

    let arg =
        if caseFields.Length = 1 then
            map caseFields.[0] |> mkExprParen
        else
            let fields = caseInfo.GetFields()

            (fields, caseFields)
            ||> Array.zip
            |> Array.map(fun (field, fieldValue) -> mkInfix (mkConstExpr field.Name) "=" (map fieldValue))
            |> Array.toList
            |> mkExprTuple
            |> mkExprParen

    ExprAppLongIdentAndSingleParenArgNode(caseName, arg, zeroRange)
    |> Expr.AppLongIdentAndSingleParenArg

and mapList (value : obj) =
    match value with
    | :? IEnumerable<obj> as enumerable -> mkListExpr [ yield! Seq.map map enumerable ]
    | :? System.Collections.IEnumerable as enumerable ->
        mkListExpr
            [
                for item in enumerable do
                    map(box item)
            ]
    | _ -> failwithf $"Excepted %A{value} to implement IEnumerable"

let getExpandedAST (ast : Fable.AST.Fable.File) : string =
    let decls = ast.Declarations |> List.map map

    let set =
        ast.UsedNamesInRootScope
        |> Set.toList
        |> List.map mkStringExpr
        |> mkArrayExpr
        |> fun set -> ExprAppNode(mkExprIdent "set", [ set ], zeroRange)
        |> Expr.App

    let fileCtor =
        ExprAppLongIdentAndSingleParenArgNode(
            IdentListNode(
                [
                    IdentifierOrDot.Ident(stn "Fable")
                    IdentifierOrDot.UnknownDot
                    IdentifierOrDot.Ident(stn "AST")
                    IdentifierOrDot.UnknownDot
                    IdentifierOrDot.Ident(stn "Fable")
                    IdentifierOrDot.UnknownDot
                    IdentifierOrDot.Ident(stn "File")
                ],
                zeroRange
            ),
            mkExprParen(mkExprTuple [ mkListExpr decls ; set ]),
            zeroRange
        )
        |> Expr.AppLongIdentAndSingleParenArg

    let oak =
        Oak([], [ ModuleOrNamespaceNode(None, [ ModuleDecl.DeclExpr fileCtor ], zeroRange) ], zeroRange)

    CodeFormatter.FormatOakAsync(
        oak,
        { FormatConfig.Default with
            MultilineBracketStyle = Stroustrup
            MaxLineLength = 400
        }
    )
    |> Async.RunSynchronously
