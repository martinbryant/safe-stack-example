module OrderTodos

open Elmish
open Shared
open Fable.Remoting.Client
open ReactDnD
open Fable.Core

type Model = { Todos: Todo list; }

type Msg =
    | GotTodos of Todo list

let todosApi =
    Remoting.createApi ()
    |> Remoting.withRouteBuilder Route.builder
    |> Remoting.buildProxy<ITodosApi>

let init () : Model * Cmd<Msg> =
    let model = { Todos = [] }

    let cmd = Cmd.OfAsync.perform todosApi.getTodos () GotTodos

    model, cmd

let update (msg: Msg) (model: Model) : Model * Cmd<Msg> =
    match msg with
    | GotTodos todos -> { model with Todos = todos }, Cmd.none

open Feliz
open Feliz.Bulma

let todoItem (model: Model) (dispatch: Msg -> unit) (todo: Todo ) =
    Bulma.box [
                                    Bulma.content [
                                        prop.text todo.Description
                                    ]
                                ]

let onDragEnd result = ()

let containerBox (model: Model) (dispatch: Msg -> unit) =
    let todoItem = todoItem model dispatch
    Bulma.box [
        Bulma.content [
            dragDropContext
                [ OnDragEnd onDragEnd ]
                [ droppable
                    [ DroppableId "droppable" ]
                    (fun (provided, snapshot) ->
                        JS.console.log snapshot
                        Html.div [
                            prop.ref provided.innerRef
                            prop.children (List.map todoItem model.Todos)
                        ])
                 ]
        ]
    ]

let view (model: Model) (dispatch: Msg -> unit) =
    Bulma.heroBody [
        Bulma.container [
            Bulma.column [
                column.is6
                column.isOffset3
                prop.children [
                    Bulma.title [
                        text.hasTextCentered
                        prop.text "todo-app"
                    ]
                    containerBox model dispatch
                ]
            ]
        ]
    ]