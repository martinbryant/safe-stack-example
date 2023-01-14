module Todo

open Shared
open Elmish
open Elmish.Navigation
open Feliz
open Feliz.Bulma
open Fable.Remoting.Client
open System

type WebData<'data, 'error> =
    | NotStarted
    | Loading
    | Loaded of 'data
    | Errored of 'error

type Model = { Todo: WebData<Todo, AppError> }

type Msg =
    | GotTodo of Result<Todo, AppError>
    | RemoveTodo of int
    | RemovedTodo of unit
    | CompleteTodo of int
    | CompletedTodo of Result<Todo, AppError>

let todosApi =
    Remoting.createApi ()
    |> Remoting.withRouteBuilder Route.builder
    |> Remoting.buildProxy<ITodosApi>

let init (id: int) : Model * Cmd<Msg> =
    let cmd = Cmd.OfAsync.perform todosApi.getTodo id GotTodo
    { Todo = Loading }, cmd

let update (msg: Msg) (model: Model) : Model * Cmd<Msg> =
    match msg with
    | GotTodo result ->
        let newModel = match result with
                        | Ok todo -> Loaded todo
                        | Error error -> Errored error
        { model with Todo = newModel}, Cmd.none
    | RemoveTodo id ->
        let cmd = Cmd.OfAsync.perform todosApi.removeTodo id RemovedTodo

        model, cmd

    | RemovedTodo _ ->
        let cmd = Navigation.newUrl "/"

        model, cmd

    | CompleteTodo id ->
        let cmd = Cmd.OfAsync.perform todosApi.completeTodo id CompletedTodo
        let model = { model with Todo = Loading }

        model, cmd

    | CompletedTodo result ->
        match result with
        | Ok todo -> { model with Todo = Loaded todo }, Cmd.none
        | Error error -> match error with
                            | NotFound -> model, Navigation.newUrl "/"
                            | _ -> { model with Todo = Errored error }, Cmd.none

let todoControls (todo: Todo) (dispatch: Msg -> unit) =
    Bulma.field.div [
        field.isGrouped
        prop.children [
            Bulma.control.p [
                Bulma.button.a [
                    color.isSuccess
                    prop.disabled todo.Completed
                    prop.onClick (fun _ -> dispatch <| CompleteTodo todo.Id)
                    prop.text "Complete"
                ]
            ]
            Bulma.control.p [
                Bulma.button.a [
                    color.isDanger
                    prop.onClick (fun _ -> dispatch <| RemoveTodo todo.Id)
                    prop.text "Delete"
                ]
            ]
        ]
    ]

let todoInfo (todo: Todo) (dispatch: Msg -> unit) =
    let createdDate =
        match todo.Created with
        | None -> "Created date unknown"
        | Some date -> sprintf "Created on %s" (date.ToShortDateString())

    Bulma.content [
        Bulma.label [
            prop.text createdDate
        ]
        todoControls todo dispatch
    ]

let todoTitle (model: Model) =
    match model.Todo with
    | Loaded todo -> todo.Description
    | _ -> String.Empty

let loadingView =
    Bulma.column [
        Bulma.progress [
            color.isPrimary
            prop.max 100
        ]
    ]

let views (model: Model) (dispatch: Msg -> unit) =
    match model.Todo with
    | Loading | NotStarted -> loadingView
    | Loaded todo -> todoInfo todo dispatch
    | Errored error ->
        match error with
            | NotFound -> Html.h1 "not found"
            | Request message -> Html.h1 message

let view (model: Model) (dispatch: Msg -> unit) =
    Bulma.heroBody [
        Bulma.container [
            Bulma.column [
                column.is6
                column.isOffset3
                prop.children [
                    Bulma.title [
                        text.hasTextCentered
                        prop.text (todoTitle model)
                    ]
                    Bulma.box [
                        views model dispatch
                    ]
                ]
            ]
        ]
    ]