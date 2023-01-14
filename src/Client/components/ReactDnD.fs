module ReactDnD

open Fable.Core
open Fable.Core.JsInterop
open Fable.React

type DraggableProvided = {
    innerRef: Browser.Types.Element -> unit
}

type DraggableStateSnapshot = {
    isDragging : bool
}

type DraggableRenderProps = (DraggableProvided * DraggableStateSnapshot) -> ReactElement

type DraggableProps =
    | Index of int
    | DraggableId of obj
    | Key of obj
    | Children of DraggableRenderProps

let inline draggable (props: DraggableProps list) (render: DraggableRenderProps) : ReactElement =
    let allProps = props @ [Children render]
    ofImport "Draggable" "react-beautiful-dnd" (keyValueList CaseRules.LowerFirst allProps) []

type DragDropContextProps =
    | OnDragEnd of (obj[] -> unit)

let inline dragDropContext (props: DragDropContextProps list) (elems: ReactElement list) : ReactElement =
    ofImport "DragDropContext" "react-beautiful-dnd" (keyValueList CaseRules.LowerFirst props) elems

type DroppableProvided = {
    innerRef: Browser.Types.Element -> unit
}

type DroppableStateSnapshot = {
    isDraggingOver : bool
}
type DroppableRenderProps = (DroppableProvided * DroppableStateSnapshot) -> ReactElement

type DroppableProps =
    | DroppableId of obj
    | Children of DroppableRenderProps


let inline droppable (props: DroppableProps list) (render: DroppableRenderProps) : ReactElement =
    let allProps = Seq.append props [Children render]
    ofImport "Droppable" "react-beautiful-dnd" (keyValueList CaseRules.LowerFirst allProps) []