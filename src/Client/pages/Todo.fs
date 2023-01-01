module Todo

open Shared
open Elmish
open Feliz
open Feliz.Bulma
open Fable.Remoting.Client

type Model = { Todo: Todo }

type Msg =
    | GotTodo of Todo

let todosApi =
    Remoting.createApi ()
    |> Remoting.withRouteBuilder Route.builder
    |> Remoting.buildProxy<ITodosApi>

let init (id: int) : Model * Cmd<Msg> =
    let cmd = Cmd.OfAsync.perform todosApi.getTodo id GotTodo
    { Todo = Todo.create "" }, cmd

let update (msg: Msg) (model: Model) : Model * Cmd<Msg> =
    match msg with
    | GotTodo todo -> { model with Todo = todo }, Cmd.none

let view (model: Model) (dispatch: Msg -> unit) =
    Bulma.heroBody [
        Bulma.container [
            Bulma.column [
                column.is6
                column.isOffset3
                prop.children [
                    Bulma.title [
                        text.hasTextCentered
                        prop.text model.Todo.Description
                    ]
                    Bulma.box [
                        Bulma.content [

                        ]
                    ]
                ]
            ]
        ]
    ]