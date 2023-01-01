module Index

open Elmish
open Urls
open Feliz

type Page =
    | TodoList of TodoList.Model
    | Todo of Todo.Model
    | NotFound

type Model =
    { CurrentPage: Page }

type Msg =
    | TodoListMsg of TodoList.Msg
    | TodoMsg of Todo.Msg

let initFromUrl url =
    match url with
    | Url.TodoList ->
        let model, cmd = TodoList.init ()
        { CurrentPage = TodoList model }, Cmd.map TodoListMsg cmd
    | Url.Todo id ->
        let model, cmd = Todo.init id
        { CurrentPage = Todo model }, Cmd.map TodoMsg cmd
    | Url.NotFound -> { CurrentPage = NotFound }, Cmd.none

let init (url: Url option): Model * Cmd<Msg> =
    match url with
    | Some url -> initFromUrl url
    | _ -> { CurrentPage = NotFound }, Cmd.none


let update (msg: Msg) (model: Model) : Model * Cmd<Msg> =
    match model.CurrentPage, msg with
    | TodoList todoModel, TodoListMsg todoMsg ->
        let todoModel, todoCmd = TodoList.update todoMsg todoModel
        let nextState = { model with CurrentPage = TodoList todoModel }
        let nextCmd = Cmd.map TodoListMsg todoCmd
        nextState, nextCmd

    | Todo todoModel, TodoMsg todoMsg ->
        let todoModel, todoCmd = Todo.update todoMsg todoModel
        let nextState = { model with CurrentPage = Todo todoModel }
        let nextCmd = Cmd.map TodoMsg todoCmd
        nextState, nextCmd
    | NotFound, _ | _, _ ->
        model, Cmd.none

let viewPage (model: Model) (dispatch: Msg -> unit) =
    match model.CurrentPage with
    | TodoList pageModel ->
        TodoList.view pageModel (dispatch << TodoListMsg)

    | Todo pageModel ->
        Todo.view pageModel (dispatch << TodoMsg)
    | NotFound -> Html.h1 "Not found"

let view (model: Model) (dispatch: Msg -> unit) =
    viewPage model dispatch
