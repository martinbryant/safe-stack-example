module Index

open Elmish
open Feliz
open Feliz.Bulma
open System
open Feliz.Router
open Feliz.style
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
    | OnLogoutRequested
    | OnLoggedIn of unit
    | OnLoggedOut of unit
    | OnInit of bool
    | UrlChanged of string list

let config: KeycloakConfig = {
    url = Env.VITE_AUTH_ORIGIN
    realm = "safe-todo"
    clientId = "todo-client"
}

let keycloak = create config

let parseUser () =
    if keycloak.authenticated then
        LoggedIn { Name = keycloak.tokenParsed.given_name }
    else
        Guest

let initFromUrl (url: string list) =
    match url with
    | [] ->
        let model, cmd = TodoList.init
        TodoList model, Cmd.map TodoListMsg cmd
    | [ "todo"; id ] ->
        let model, cmd = Todo.init (Guid.Parse id)
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

let update msg model =
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
    | _, OnLogoutRequested ->
        let loginConfig = {
            redirectUri = window.location.href
        }
        model, Cmd.OfPromise.perform keycloak.logout loginConfig OnLoggedOut
    | _, OnLoggedIn _ ->
        { model with User = user }, Cmd.none
    | _, OnLoggedOut _ ->
        { model with User = user }, Cmd.none
    | NotFound, _
    | _, _ -> model, Cmd.none

let viewPage model dispatch =
    match model.CurrentPage with
    | TodoList pageModel -> TodoList.view pageModel (dispatch << TodoListMsg)

    | Todo pageModel -> Todo.view pageModel (dispatch << TodoMsg)
    | NotFound -> Html.h1 "Not found"

let navBrand =
    Bulma.navbarBrand.div [
        color.hasBackgroundPrimary
        prop.children [
            Bulma.navbarItem.a [
                prop.href "https://safe-stack.github.io/"
                prop.children [
                    Html.img [ prop.src "/favicon.png"; prop.alt "Logo" ]
                ]
            ]
        ]
    ]

let login user dispatch =
    let userText, dropdownActionText, onClick =
        match user with
        | Guest ->
            "guest",
            "Login",
            (fun _ -> dispatch OnLoginRequested)
        | LoggedIn session ->
            session.Name,
            "Logout",
            (fun _ -> dispatch OnLogoutRequested)
    Bulma.navbarMenu [
        Bulma.navbarEnd.div [
            prop.children [
                Bulma.navbarItem.div [
                    navbarItem.hasDropdown
                    navbarItem.isHoverable
                    color.isPrimary
                    color.hasBackgroundPrimary
                    prop.children [
                        Bulma.navbarLink.a [
                            navbarLink.isArrowless
                            prop.text $"Hi {userText}"
                        ]
                        Bulma.navbarDropdown.div [
                            prop.style [
                                backgroundColor.transparent
                                borderStyle.none
                            ]
                            prop.children [
                                Bulma.navbarItem.a [
                                    prop.onClick onClick
                                    prop.text dropdownActionText
                                ]
                            ]
                        ]
                    ]
                ]
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
                        prop.style [
                            style.margin (0,40)
                        ]
                        prop.children [
                            Bulma.navbar [
                                navbar.isTransparent
                                prop.children [
                                    navBrand; login model.User dispatch
                                ]
                            ]
                        ]
                    ]
                    Bulma.heroBody [ viewPage model dispatch ]
                ]
            ]
        ]
    ]