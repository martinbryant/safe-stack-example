module TodoList

open Elmish
open Elmish.Navigation
open Fable.Remoting.Client
open Shared
open System


type Model = { Todos: Todo list; Input: string; ShowDeleted: bool }

type Msg =
    | GotTodos of Todo list
    | SetInput of string
    | AddTodo
    | AddedTodo of Todo
    | TodoClicked of Guid
    | ToggleShowDeleted

let todosApi =
    Remoting.createApi ()
    |> Remoting.withRouteBuilder Route.builder
    |> Remoting.buildProxy<ITodosApi>

let init () : Model * Cmd<Msg> =
    let model = { Todos = []; Input = ""; ShowDeleted = false }

    let cmd = Cmd.OfAsync.perform todosApi.getTodos () GotTodos

    model, cmd

let update (msg: Msg) (model: Model) : Model * Cmd<Msg> =
    match msg with
    | GotTodos todos -> { model with Todos = todos }, Cmd.none
    | SetInput value -> { model with Input = value }, Cmd.none
    | AddTodo ->
        let todo = Todo.create model.Input

        let cmd = Cmd.OfAsync.perform todosApi.addTodo todo AddedTodo

        { model with Input = "" }, cmd
    | AddedTodo todo -> { model with Todos = (Seq.toList model.Todos) @ [ todo ] }, Cmd.none
    | TodoClicked id ->
        let url = "todo" + "/" + id.ToString()
        let cmd = Navigation.newUrl url
        model, cmd
    | ToggleShowDeleted ->
        { model with ShowDeleted = not model.ShowDeleted }, Cmd.none


open Feliz
open Feliz.Bulma

let todoItem (model: Model) (dispatch: Msg -> unit) (todo: Todo ) =
    let clickTodo = fun _ -> todo.Id |> TodoClicked |> dispatch
    let completeStyle =
        if todo.Completed.IsSome || todo.Deleted.IsSome
            then
                [ "strikethrough" ]
            else
                []

    Html.li [
        Html.a [
            prop.classes completeStyle
            prop.onClick clickTodo
            prop.text todo.Description ]
        ]

let containerBox (model: Model) (dispatch: Msg -> unit) =
    let todoItem = todoItem model dispatch
    Bulma.box [
        Bulma.content [
            Html.ol (model.Todos
                     |> List.filter (fun todo -> todo.Deleted.IsNone || model.ShowDeleted)
                     |> List.map todoItem)
        ]
        Bulma.field.div [
            field.isGrouped
            prop.children [
                Bulma.control.p [
                    control.isExpanded
                    prop.children [
                        Bulma.input.text [
                            prop.value model.Input
                            prop.placeholder "What needs to be done?"
                            prop.onChange (fun x -> SetInput x |> dispatch)
                        ]
                    ]
                ]
                Bulma.control.p [
                    Bulma.button.a [
                        color.isPrimary
                        prop.disabled (Todo.isValid model.Input |> not)
                        prop.onClick (fun _ -> dispatch AddTodo)
                        prop.text "Add"
                    ]
                ]
            ]
        ]
        Bulma.field.div [
            prop.children [
                Switch.checkbox [
                        prop.id "deleted-toggle"
                        color.isSuccess
                        switch.isRounded
                        prop.onClick (fun _ -> dispatch ToggleShowDeleted)
                    ]
                Html.label [
                    prop.htmlFor "deleted-toggle"
                    prop.text " Show deleted?"
                ]
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