module Index

open Elmish
open Fable.Remoting.Client
open Feliz
open Feliz.Bulma
open System
open Feliz.Router
open Fable.Core
open Keycloak
open Session
open Shared
open Browser


type Page =
    | TodoList of TodoList.Model
    | Todo of Todo.Model
    | NotFound

type Model = { CurrentPage: Page; User: User }

type Msg =
    | TodoListMsg of TodoList.Msg
    | TodoMsg of Todo.Msg
    | OnLoginRequested
    | OnLoggedIn of unit
    | OnInit of bool
    | UrlChanged of string list

let config: KeycloakConfig = {
    url = "http://localhost:7080"
    realm = "safe-todo"
    clientId = "todo-client"

}

let authTodosApi token =
    let bearer = $"Bearer {token}"
    Remoting.createApi ()
    |> Remoting.withAuthorizationHeader bearer
    |> Remoting.withRouteBuilder Route.builder
    |> Remoting.buildProxy<IAuthTodosApi>

let keycloak = create config

let parseUser () =
    if keycloak.authenticated then
        LoggedIn { Name = keycloak.tokenParsed.given_name }
    else
        Guest

let initFromUrl (url: string list) =
    let user = parseUser()

    match url with
    | [] ->
        let model, cmd = TodoList.init user
        TodoList model, Cmd.map TodoListMsg cmd
    | [ "todo"; id ] ->
        let model, cmd = Todo.init user (Guid.Parse id)
        Todo model, Cmd.map TodoMsg cmd
    | _ -> NotFound, Cmd.none

let init () =
    let page, pageCmd = Router.currentUrl () |> initFromUrl

    let config = {
        onLoad = "check-sso"
        silentCheckSsoRedirectUri = $"{window.location.origin}/silent-check-sso.html"
        enableLogging = true
        responseMode = "query"
    }

    let initCmd = Cmd.OfPromise.perform keycloak.init config OnInit

    let user = parseUser()

    let cmd = Cmd.batch [ pageCmd; initCmd ]

    { CurrentPage = page; User = user }, cmd

let update (msg: Msg) (model: Model) : Model * Cmd<Msg> =
    let user = parseUser()

    match model.CurrentPage, msg with
    | TodoList todoModel, TodoListMsg todoMsg ->
        let todoModel, todoCmd = TodoList.update keycloak.token todoMsg todoModel

        let nextState = {
            model with
                CurrentPage = TodoList todoModel
        }

        let nextCmd = Cmd.map TodoListMsg todoCmd
        nextState, nextCmd

    | Todo todoModel, TodoMsg todoMsg ->
        let todoModel, todoCmd = Todo.update keycloak.token todoMsg todoModel

        let nextState = {
            model with
                CurrentPage = Todo todoModel
        }

        let nextCmd = Cmd.map TodoMsg todoCmd
        nextState, nextCmd
    | _, UrlChanged url ->
        let page, cmd = initFromUrl url

        { model with CurrentPage = page }, cmd
    | _, OnInit _->
        { model with User = user }, Cmd.none
    | _, OnLoginRequested ->
        let loginConfig = {
            redirectUri = window.location.href
        }
        model, Cmd.OfPromise.perform keycloak.login loginConfig OnLoggedIn
    | _, OnLoggedIn _ ->
        { model with User = user }, Cmd.none
    | NotFound, _
    | _, _ -> model, Cmd.none

let viewPage (model: Model) (dispatch: Msg -> unit) =
    match model.CurrentPage with
    | TodoList pageModel -> TodoList.view pageModel (dispatch << TodoListMsg)

    | Todo pageModel -> Todo.view pageModel (dispatch << TodoMsg)
    | NotFound -> Html.h1 "Not found"

let navBrand =
    Bulma.navbarBrand.div [
        Bulma.navbarItem.a [
            prop.href "https://safe-stack.github.io/"
            navbarItem.isActive
            prop.children [
                Html.img [ prop.src "/favicon.png"; prop.alt "Logo" ]
            ]
        ]
    ]

let login user dispatch =
    let children =
        match user with
        | Guest ->
            Html.img [ prop.src "/favicon.png"; prop.alt "Logo" ]
        | LoggedIn session ->
            Html.label [ prop.text session.Name ]
    Bulma.navbarBrand.div [
        Bulma.navbarItem.a [
            prop.onClick (fun _ -> dispatch OnLoginRequested)
            navbarItem.isActive
            prop.children [
                children
            ]
        ]
    ]

let view (model: Model) (dispatch: Msg -> unit) =
    React.router [
        router.onUrlChanged (UrlChanged >> dispatch)
        router.children [
            Bulma.hero [
                hero.isFullHeight
                color.isPrimary
                prop.style [
                    style.backgroundSize "cover"
                    style.backgroundImageUrl
                        "https://unsplash.it/1200/900?random"
                    style.backgroundPosition "no-repeat center center fixed"
                ]
                prop.children [
                    Bulma.heroHead [
                        Bulma.navbar [ Bulma.container [ navBrand; login model.User dispatch ] ]
                    ]
                    Bulma.heroBody [ viewPage model dispatch ]
                ]
            ]
        ]
    ]