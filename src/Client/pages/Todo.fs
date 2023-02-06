module Todo

open System
open Shared
open Elmish
open Elmish.Navigation
open Feliz
open Feliz.Bulma
open Fable.Remoting.Client

type WebData<'data, 'error> =
    | NotStarted
    | Loading
    | Loaded of 'data
    | Errored of 'error

type ConfirmationOpen =
    | Open of Guid
    | Closed

type Model = { Todo: WebData<Todo, AppError>; IsConfirmationOpen: ConfirmationOpen }

type Msg =
    | GotTodo of Result<Todo, AppError>
    | RemoveTodo of Guid
    | RemovedTodo of unit
    | CompleteTodo of Guid
    | CompletedTodo of Result<Todo, AppError>
    | RequestRemove of Guid
    | CancelRemoveRequest

let todosApi =
    Remoting.createApi ()
    |> Remoting.withRouteBuilder Route.builder
    |> Remoting.buildProxy<ITodosApi>

let init (id: Guid) : Model * Cmd<Msg> =
    let cmd = Cmd.OfAsync.perform todosApi.getTodo id GotTodo
    { Todo = Loading; IsConfirmationOpen = Closed }, cmd

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

    | RequestRemove id ->
        let model = { model with IsConfirmationOpen = Open id }

        model, Cmd.none

    | CancelRemoveRequest ->
        let model = { model with IsConfirmationOpen = Closed }

        model, Cmd.none

let confirmationModal (model: Model) (dispatch: Msg -> unit) =
    let id =
        match model.IsConfirmationOpen with
        | Open id -> id
        | Closed -> Guid.Empty

    Bulma.modal [
        prop.id "modal"
        if model.IsConfirmationOpen <> Closed then modal.isActive
        prop.children [
            Bulma.modalBackground []
            Bulma.modalContent [
                Bulma.box [
                    Html.h1 "Are you sure you want to remove this?"
                    Bulma.field.div [
                        field.isGrouped
                        prop.children [
                            Bulma.control.p [
                                Bulma.button.a [
                                    color.isDanger
                                    prop.onClick (fun _ -> dispatch <| RemoveTodo id)
                                    prop.text "Confirm"
                                ]
                            ]
                            Bulma.control.p [
                                Bulma.button.a [
                                    color.isLight
                                    prop.onClick (fun _ -> dispatch <| CancelRemoveRequest)
                                    prop.text "Cancel"
                                ]
                            ]
                        ]
                    ]
                ]
            ]
        ]
    ]

let todoControls (todo: Todo) (dispatch: Msg -> unit) =
    Bulma.field.div [
        field.isGrouped
        prop.children [
            Bulma.control.p [
                Bulma.button.a [
                    color.isSuccess
                    prop.disabled (todo.Completed.IsSome || todo.Deleted.IsSome)
                    prop.onClick (fun _ -> dispatch <| CompleteTodo todo.Id)
                    prop.text "Complete"
                ]
            ]
            Bulma.control.p [
                Bulma.button.a [
                    color.isDanger
                    prop.disabled todo.Deleted.IsSome
                    prop.onClick (fun _ -> dispatch <| RequestRemove todo.Id)
                    prop.text "Delete"
                ]
            ]
        ]
    ]

let todoInfo (todo: Todo) (dispatch: Msg -> unit) =
    let createdDate = sprintf "Created on %s" (todo.Created.ToString("d"))

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

let timeline =
    Timeline.timeline [
        timeline.isCentered
        prop.children [
            Timeline.header [
                Bulma.tag [ color.isPrimary; tag.isMedium; prop.text "Start" ]
            ]
            Timeline.item [
                Timeline.marker []
                Timeline.content [
                    Timeline.content.header "Feliz created"
                    Timeline.content.content "A low of cool things can happen now"
                ]
            ]
            Timeline.item [
                Timeline.marker [
                    marker.isImage
                    image.is32x32
                    prop.children [ Html.img [ prop.src "https://via.placeholder.com/32" ] ]
                ]
                Timeline.content [
                    Timeline.content.header "Feliz.Bulma started"
                    Timeline.content.content "Now we can do cool stuff with fancy styles"
                ]
            ]
            Timeline.header [
                Bulma.tag [ color.isPrimary; prop.text "2020" ]
            ]
            Timeline.item [
                Timeline.marker [
                    marker.isIcon
                    prop.children [ Html.i [ prop.className "fas fa-flag" ] ]
                ]
                Timeline.content [
                    Timeline.content.header "Fable 3 released"
                    Timeline.content.content "What can be better than .NET tool compiling F# to JS?"
                ]
            ]
            Timeline.header [
                Bulma.tag [ color.isPrimary; tag.isMedium; prop.text "End" ]
            ]

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
            Bulma.column [
                column.is12
                prop.children [
                    Bulma.box [
                        timeline
                    ]
                ]
            ]
        ]
        confirmationModal model dispatch
    ]