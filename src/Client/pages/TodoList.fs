module TodoList

open Elmish
open Fable.Remoting.Client
open Shared
open System
open Feliz.Router


type Model = {
    Todos: Todo list
    Input: string
    ShowDeleted: bool
}

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

let authTodosApi =
    Remoting.createApi ()
    |> Remoting.withAuthorizationHeader "Bearer eyJhbGciOiJSUzI1NiIsInR5cCIOiAiSldUIiwia2lkIiA6ICJMVmpOcEhhenQ3VkF4SXhOa0lxV3gybW1DZzh4NG9reDJoRWRwOF82LTU0In0.eyJleHAiOjE3NDMxNzU0MjcsImlhdCI6MTc0MzE3NTEyNywianRpIjoiNGI4ZDkxYjYtZDk2ZC00MDFmLTk4ZWEtOTIxZGZkYWVjMTM3IiwiaXNzIjoiaHR0cDovL2xvY2FsaG9zdDo3MDgwL3JlYWxtcy9zYWZlLXRvZG8iLCJhdWQiOiJhY2NvdW50Iiwic3ViIjoiZGU3YmZjNWYtMTIwOS00OGFhLTgwODQtNWJkNzFlN2UyZWUyIiwidHlwIjoiQmVhcmVyIiwiYXpwIjoidG9kby1jbGllbnQiLCJzaWQiOiJkNTgyNWU3Zi01YzA4LTQzYTctOGMxOS0yNmZiMDdmNTdkMzQiLCJhY3IiOiIxIiwiYWxsb3dlZC1vcmlnaW5zIjpbIi8qIl0sInJlYWxtX2FjY2VzcyI6eyJyb2xlcyI6WyJvZmZsaW5lX2FjY2VzcyIsInVtYV9hdXRob3JpemF0aW9uIiwiZGVmYXVsdC1yb2xlcy1zYWZlLXRvZG8iXX0sInJlc291cmNlX2FjY2VzcyI6eyJhY2NvdW50Ijp7InJvbGVzIjpbIm1hbmFnZS1hY2NvdW50IiwibWFuYWdlLWFjY291bnQtbGlua3MiLCJ2aWV3LXByb2ZpbGUiXX19LCJzY29wZSI6Im9wZW5pZCBwcm9maWxlIGVtYWlsIiwiZW1haWxfdmVyaWZpZWQiOnRydWUsIm5hbWUiOiJNYXJ0aW4gQnJ5YW50IiwicHJlZmVycmVkX3VzZXJuYW1lIjoibWFydGluIiwiZ2l2ZW5fbmFtZSI6Ik1hcnRpbiIsImZhbWlseV9uYW1lIjoiQnJ5YW50IiwiZW1haWwiOiJtYXJ0aW5icnlhbnQuZGV2QGdtYWlsLmNvbSJ9.bXNcKtNBaxgIXM9KiEHn7E1ai77xDvqYB52SW5Az2YdUqLlO41UMyRrD6d0b-lFqxqBfnnAbJr14imeGHldqT8_nkpfiV8hUNtH88mYdN15mPnyurq0CMKNwcKrH7TY9W_FDJl74pASmTfqoY01pQf6zpfoVaBjJWSThudnRgMURHOpEcTHtBYiT6rm1nbH7C23GWONOszwRe-M7ExnhPfY5NPvlmZgPkDEkZUN-uCb85ov0SwCc1ag9ybgihcINZ1wA8AOypezakwH-KBbzYrOsaCV61ZafyJLO17zF4uZKeV29aKasse86G_kFxv6Wpl_PoTk-yezhUa2sb0SW8Q"
    |> Remoting.withRouteBuilder Route.builder
    |> Remoting.buildProxy<IAuthTodosApi>

let init () : Model * Cmd<Msg> =
    let model = {
        Todos = []
        Input = ""
        ShowDeleted = false
    }

    let cmd = Cmd.OfAsync.perform todosApi.getTodos () GotTodos

    model, cmd

let update (msg: Msg) (model: Model) : Model * Cmd<Msg> =
    match msg with
    | GotTodos todos -> { model with Todos = todos }, Cmd.none
    | SetInput value -> { model with Input = value }, Cmd.none
    | AddTodo ->
        let todo = Todo.create model.Input

        let cmd = Cmd.OfAsync.perform authTodosApi.addTodo todo AddedTodo

        { model with Input = "" }, cmd
    | AddedTodo todo ->
        {
            model with
                Todos = (Seq.toList model.Todos) @ [ todo ]
        },
        Cmd.none
    | TodoClicked id ->
        let url = Router.formatPath ("todo", id.ToString())
        let cmd = Cmd.navigate url
        model, cmd
    | ToggleShowDeleted ->
        {
            model with
                ShowDeleted = not model.ShowDeleted
        },
        Cmd.none


open Feliz
open Feliz.Bulma

let todoItem (model: Model) (dispatch: Msg -> unit) (todo: Todo) =
    let clickTodo = fun _ -> todo.Id |> TodoClicked |> dispatch

    let strikethrough =
        if todo.Completed.IsSome || todo.Deleted.IsSome then
            [ style.textDecoration.lineThrough ]
        else
            []

    let deletedColour =
        if todo.Deleted.IsSome then [ color.hasTextDanger ] else []

    Html.li [
        Html.a (
            [
                prop.onClick clickTodo
                prop.text todo.Description
                prop.style strikethrough
            ]
            @ deletedColour
        )
    ]

let containerBox (model: Model) (dispatch: Msg -> unit) =
    let todoItem = todoItem model dispatch

    Bulma.box [
        Bulma.content [
            Html.ol (
                model.Todos
                |> List.filter (fun todo ->
                    todo.Deleted.IsNone || model.ShowDeleted)
                |> List.map todoItem
            )
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
                    Bulma.title [ text.hasTextCentered; prop.text "todo-app" ]
                    containerBox model dispatch
                ]
            ]
        ]
    ]