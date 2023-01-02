module Todo

open Shared
open Elmish
open Feliz
open Feliz.Bulma
open Fable.Remoting.Client

type WebData<'data, 'error> =
    | NotStarted
    | Loading
    | Loaded of 'data
    | Errored of 'error

type Model = { Todo: WebData<Todo, AppError> }

type Msg =
    | GotTodo of Result<Todo, AppError>

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

let todoView (todo: Todo) =
    Bulma.heroBody [
        Bulma.container [
            Bulma.column [
                column.is6
                column.isOffset3
                prop.children [
                    Bulma.title [
                        text.hasTextCentered
                        prop.text todo.Description
                    ]
                    Bulma.box [
                        Bulma.content [

                        ]
                    ]
                ]
            ]
        ]
    ]

let view (model: Model) (dispatch: Msg -> unit) =
    match model.Todo with
    | Loading | NotStarted -> Html.h1 "...loading"
    | Loaded todo -> todoView todo
    | Errored error ->
        match error with
            | NotFound -> Html.h1 "not found"
            | Request message -> Html.h1 message