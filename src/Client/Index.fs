module Index

open Elmish
open Urls
open Feliz
open Feliz.Bulma

type Page =
    | TodoPage of TodoPage.Model
    | NotFound

type Model =
    { CurrentPage: Page }

type Msg =
    TodoPageMsg of TodoPage.Msg

let initFromUrl url =
    match url with
    | Url.Todo ->
        let model, cmd = TodoPage.init ()
        { CurrentPage = TodoPage model }, Cmd.map TodoPageMsg cmd
    | Url.NotFound -> { CurrentPage = NotFound }, Cmd.none

let init (url: Url option): Model * Cmd<Msg> =
    match url with
    | Some url -> initFromUrl url
    | _ -> { CurrentPage = NotFound }, Cmd.none


let update (msg: Msg) (model: Model) : Model * Cmd<Msg> =
    match model.CurrentPage, msg with
    | TodoPage todoModel, TodoPageMsg todoMsg ->
        let todoModel, todoCmd = TodoPage.update todoMsg todoModel
        let nextState = { model with CurrentPage = TodoPage todoModel }
        let nextCmd = Cmd.map TodoPageMsg todoCmd
        nextState, nextCmd
    | NotFound, _ ->
        model, Cmd.none

let viewPage (model: Model) (dispatch: Msg -> unit) =
    match model.CurrentPage with
    | TodoPage pageModel ->
        TodoPage.view pageModel (dispatch << TodoPageMsg)
    | NotFound -> Html.h1 "Not found"

let view (model: Model) (dispatch: Msg -> unit) =
    viewPage model dispatch
