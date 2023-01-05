module ReactDnD

open Fable.Core
open Fable.Core.JsInterop
open Fable.React
open Fable.React.Props

type DraggableProps =
    | Index of int
    | DraggableId of obj

let inline draggable (props: DraggableProps) (elems: ReactElement list) : ReactElement =
    ofImport "draggable" "react-beautiful-dnd" (keyValueList CaseRules.LowerFirst props) elems