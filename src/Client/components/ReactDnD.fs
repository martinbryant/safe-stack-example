module ReactDnD

open Fable.Core
open Fable.Core.JsInterop
open Fable.React

type DraggableProps =
    | Index of int
    | DraggableId of obj
    | Key of obj

let inline draggable (props: DraggableProps list) (elems: ReactElement list) : ReactElement =
    ofImport "Draggable" "react-beautiful-dnd" (keyValueList CaseRules.LowerFirst props) elems

type DragDropContextProps =
    | OnDragEnd of (obj[] -> unit)

let inline dragDropContext (props: DragDropContextProps list) (elems: ReactElement list) : ReactElement =
    ofImport "DragDropContext" "react-beautiful-dnd" (keyValueList CaseRules.LowerFirst props) elems

type DroppableProps =
    | DroppableId of obj

type DroppableStateSnapshot = {
    isDraggingOver : bool
}

type DroppableProvided = {
    innerRef : Browser.Types.Element -> unit
}

let inline droppable (props: DroppableProps list) (elem: (DroppableProps * DroppableStateSnapshot) -> ReactElement) : ReactElement =
    let children = elem
    ofImport "Droppable" "react-beautiful-dnd" (keyValueList CaseRules.LowerFirst props) [ elem ]