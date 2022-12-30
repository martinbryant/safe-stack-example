module Index

open Elmish
open Feliz
open Feliz.Router

type Page =
    | TodoPage of TodoPage.Model

type Model =
    { CurrentPage: Page; CurrentUrl: string list }

type Msg =
    | UrlChanged of string list
    | TodoPageMsg of TodoPage.Msg

let init () : Model * Cmd<Msg> =
    let model, cmd = TodoPage.init ()

    { CurrentPage = TodoPage model; CurrentUrl = Router.currentUrl() }, Cmd.map TodoPageMsg cmd

let update (msg: Msg) (model: Model) : Model * Cmd<Msg> =
    match model.CurrentPage, msg with
    | _, UrlChanged url ->
        { model with CurrentUrl = url }, Cmd.none
    | TodoPage todoModel, TodoPageMsg todoMsg ->
        let todoModel, todoCmd = TodoPage.update todoMsg todoModel
        let nextState = { model with CurrentPage = TodoPage todoModel }
        let nextCmd = Cmd.map TodoPageMsg todoCmd
        nextState, nextCmd

let viewPage (model: Model) (dispatch: Msg -> unit) =
    match model.CurrentPage with
    | TodoPage pageModel ->
        TodoPage.view pageModel (dispatch << TodoPageMsg)

let view (model: Model) (dispatch: Msg -> unit) =
    React.router [
        router.onUrlChanged (UrlChanged >> dispatch)
        router.children [ viewPage model dispatch ]
    ]
