namespace Fabulous.Avalonia

open System.Runtime.CompilerServices
open Avalonia.Input
open AvaloniaEdit
open AvaloniaEdit.Document
open Fabulous
open Fabulous.Avalonia
open Fabulous.StackAllocatedCollections.StackList

type IFabTextEditor =
    inherit IFabTemplatedControl

module TextEditor =
    let WidgetKey = Widgets.register<TextEditor>()

    let Document =
        Attributes.defineAvaloniaPropertyWithEquality TextEditor.DocumentProperty

    let Options =
        Attributes.defineAvaloniaPropertyWithEquality TextEditor.OptionsProperty

    let TextChanged =
        Attributes.defineEvent<TextInputEventArgs>
            "TextChanged"
            (fun target -> (target :?> TextEditor).TextArea.TextEntered)

    let FontFamily =
        Attributes.defineAvaloniaPropertyWithEquality TextEditor.FontFamilyProperty

    let ShowLineNumbers =
        Attributes.defineAvaloniaPropertyWithEquality TextEditor.ShowLineNumbersProperty

    let FontSize =
        Attributes.defineAvaloniaPropertyWithEquality TextEditor.FontSizeProperty

    let IsReadOnly =
        Attributes.defineAvaloniaPropertyWithEquality TextEditor.IsReadOnlyProperty

[<AutoOpen>]
module TextEditorBuilders =
    type Fabulous.Avalonia.View with

        /// <summary>Creates a TextEditor widget.</summary>
        static member TextEditor (?options : TextEditorOptions) =
            let options = Option.defaultValue (TextEditorOptions()) options

            WidgetBuilder<'msg, IFabTextEditor>(
                TextEditor.WidgetKey,
                AttributesBundle(StackList.one(TextEditor.Options.WithValue options), ValueNone, ValueNone)
            )

[<Extension>]
type TextEditorModifiers =

    [<Extension>]
    static member inline fontFamily (this : WidgetBuilder<'msg, #IFabTextEditor>, value : string) =
        this.AddScalar(TextEditor.FontFamily.WithValue value)

    [<Extension>]
    static member inline showLineNumber (this : WidgetBuilder<'msg, #IFabTextEditor>, value : bool) =
        this.AddScalar(TextEditor.ShowLineNumbers.WithValue value)

    [<Extension>]
    static member inline fontSize (this : WidgetBuilder<'msg, #IFabTextEditor>, value : float) =
        this.AddScalar(TextEditor.FontSize.WithValue value)

    [<Extension>]
    static member inline onTextChanged
        (this : WidgetBuilder<'msg, #IFabTextEditor>, callback : TextInputEventArgs -> 'msg)
        =
        this.AddScalar(TextEditor.TextChanged.WithValue callback)

    [<Extension>]
    static member inline isReadOnly (this : WidgetBuilder<'msg, #IFabTextEditor>, value : bool) =
        this.AddScalar(TextEditor.IsReadOnly.WithValue value)

    [<Extension>]
    static member inline text (this : WidgetBuilder<'msg, #IFabTextEditor>, value : string) =
        let document = TextDocument()
        document.Text <- value
        this.AddScalar(TextEditor.Document.WithValue document)
