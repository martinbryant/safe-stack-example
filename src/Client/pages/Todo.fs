module Todo

open System
open Shared
open Elmish
open Feliz
open Feliz.Bulma
open Feliz.Router
open Fable.Remoting.Client
open Fable.DateFunctions


type WebData<'data, 'error> =
    | NotStarted
    | Loading
    | Loaded of 'data
    | Errored of 'error

type ConfirmationOpen =
    | Open of Guid
    | Closed

type Model = {
    Todo: WebData<Todo, AppError>
    IsConfirmationOpen: ConfirmationOpen
    History: TodoHistoryItem list
}

type Msg =
    | GotTodo of Result<Todo, AppError>
    | GotHistory of TodoHistoryItem list
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

let authTodosApi =
    Remoting.createApi ()
    |> Remoting.withAuthorizationHeader "Bearer eyJhbGcOiJSUzI1NiIsInR5cCIgOiAildUIiwia2lkIiA6ICJMVmpOcEhhenQ3VkF4SXhOa0lxV3gybW1DZzh4NG9reDJoRWRwOF82LTU0In0.eyJleHAiOjE3NDMxNzU0MjcsImlhdCI6MTc0MzE3NTEyNywianRpIjoiNGI4ZDkxYjYtZDk2ZC00MDFmLTk4ZWEtOTIxZGZkYWVjMTM3IiwiaXNzIjoiaHR0cDovL2xvY2FsaG9zdDo3MDgwL3JlYWxtcy9zYWZlLXRvZG8iLCJhdWQiOiJhY2NvdW50Iiwic3ViIjoiZGU3YmZjNWYtMTIwOS00OGFhLTgwODQtNWJkNzFlN2UyZWUyIiwidHlwIjoiQmVhcmVyIiwiYXpwIjoidG9kby1jbGllbnQiLCJzaWQiOiJkNTgyNWU3Zi01YzA4LTQzYTctOGMxOS0yNmZiMDdmNTdkMzQiLCJhY3IiOiIxIiwiYWxsb3dlZC1vcmlnaW5zIjpbIi8qIl0sInJlYWxtX2FjY2VzcyI6eyJyb2xlcyI6WyJvZmZsaW5lX2FjY2VzcyIsInVtYV9hdXRob3JpemF0aW9uIiwiZGVmYXVsdC1yb2xlcy1zYWZlLXRvZG8iXX0sInJlc291cmNlX2FjY2VzcyI6eyJhY2NvdW50Ijp7InJvbGVzIjpbIm1hbmFnZS1hY2NvdW50IiwibWFuYWdlLWFjY291bnQtbGlua3MiLCJ2aWV3LXByb2ZpbGUiXX19LCJzY29wZSI6Im9wZW5pZCBwcm9maWxlIGVtYWlsIiwiZW1haWxfdmVyaWZpZWQiOnRydWUsIm5hbWUiOiJNYXJ0aW4gQnJ5YW50IiwicHJlZmVycmVkX3VzZXJuYW1lIjoibWFydGluIiwiZ2l2ZW5fbmFtZSI6Ik1hcnRpbiIsImZhbWlseV9uYW1lIjoiQnJ5YW50IiwiZW1haWwiOiJtYXJ0aW5icnlhbnQuZGV2QGdtYWlsLmNvbSJ9.bXNcKtNBaxgIXM9KiEHn7E1ai77xDvqYB52SW5Az2YdUqLlO41UMyRrD6d0b-lFqxqBfnnAbJr14imeGHldqT8_nkpfiV8hUNtH88mYdN15mPnyurq0CMKNwcKrH7TY9W_FDJl74pASmTfqoY01pQf6zpfoVaBjJWSThudnRgMURHOpEcTHtBYiT6rm1nbH7C23GWONOszwRe-M7ExnhPfY5NPvlmZgPkDEkZUN-uCb85ov0SwCc1ag9ybgihcINZ1wA8AOypezakwH-KBbzYrOsaCV61ZafyJLO17zF4uZKeV29aKasse86G_kFxv6Wpl_PoTk-yezhUa2sb0SW8Q"
    |> Remoting.withRouteBuilder Route.builder
    |> Remoting.buildProxy<IAuthTodosApi>

let getHistory id =
    Cmd.OfAsync.perform todosApi.getHistory id GotHistory

let init (id: Guid) : Model * Cmd<Msg> =
    let cmd = Cmd.OfAsync.perform todosApi.getTodo id GotTodo

    {
        Todo = Loading
        IsConfirmationOpen = Closed
        History = []
    },
    cmd

let update (msg: Msg) (model: Model) : Model * Cmd<Msg> =
    match msg with
    | GotTodo result ->
        let newModel =
            match result with
            | Ok todo -> Loaded todo
            | Error error -> Errored error

        let cmd =
            match result with
            | Ok todo -> getHistory todo.Id
            | Error _ -> Cmd.none

        { model with Todo = newModel }, cmd
    | GotHistory history -> { model with History = history }, Cmd.none

    | RemoveTodo id ->
        let cmd = Cmd.OfAsync.perform authTodosApi.removeTodo id RemovedTodo

        model, cmd

    | RemovedTodo _ ->
        let cmd = Cmd.navigate "/"

        model, cmd

    | CompleteTodo id ->
        let cmd = Cmd.OfAsync.perform authTodosApi.completeTodo id CompletedTodo
        let model = { model with Todo = Loading }

        model, cmd

    | CompletedTodo result ->
        match result with
        | Ok todo -> { model with Todo = Loaded todo }, getHistory todo.Id
        | Error error ->
            match error with
            | NotFound -> model, Cmd.navigate "/"
            | _ -> { model with Todo = Errored error }, Cmd.none

    | RequestRemove id ->
        let model = {
            model with
                IsConfirmationOpen = Open id
        }

        model, Cmd.none

    | CancelRemoveRequest ->
        let model = {
            model with
                IsConfirmationOpen = Closed
        }

        model, Cmd.none

let confirmationModal (model: Model) (dispatch: Msg -> unit) =
    let id =
        match model.IsConfirmationOpen with
        | Open id -> id
        | Closed -> Guid.Empty

    Bulma.modal [
        prop.id "modal"
        if model.IsConfirmationOpen <> Closed then
            modal.isActive
        prop.children [
            Bulma.modalBackground []
            Bulma.modalContent [
                Bulma.box [
                    Html.h1 "Are you sure you want to remove this?"
                    Bulma.field.div [
                        field.isGrouped
                        prop.children [
                            Bulma.control.p [
                                Bulma.button.button [
                                    color.isDanger
                                    prop.onClick (fun _ ->
                                        dispatch <| RemoveTodo id)
                                    prop.text "Confirm"
                                ]
                            ]
                            Bulma.control.p [
                                Bulma.button.button [
                                    color.isLight
                                    prop.onClick (fun _ ->
                                        dispatch <| CancelRemoveRequest)
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
                Bulma.button.button [
                    color.isSuccess
                    prop.disabled (todo.Completed.IsSome || todo.Deleted.IsSome)
                    prop.onClick (fun _ -> dispatch <| CompleteTodo todo.Id)
                    prop.text "Complete"
                ]
            ]
            Bulma.control.p [
                Bulma.button.button [
                    color.isDanger
                    prop.disabled todo.Deleted.IsSome
                    prop.onClick (fun _ -> dispatch <| RequestRemove todo.Id)
                    prop.text "Delete"
                ]
            ]
        ]
    ]

let formatDate (date: DateTimeOffset) =
    date.Format("EEEE, do MMMM y 'at' H:mm")

let createdSection event =
    Timeline.item [
        Timeline.marker [
            marker.isIcon
            color.isInfo
            prop.children [ Html.i [ prop.className "fas fa-plus" ] ]
        ]
        Timeline.content [
            Timeline.content.header <| TodoEvent.toString event.Event
            Timeline.content.content (formatDate event.At)
        ]
    ]

let completeSection event =
    Timeline.item [
        Timeline.marker [
            marker.isIcon
            color.isPrimary
            prop.children [ Html.i [ prop.className "fas fa-check" ] ]
        ]
        Timeline.content [
            Timeline.content.header <| TodoEvent.toString event.Event
            Timeline.content.content (formatDate event.At)
        ]
    ]

let deletedSection historyItem =
    Timeline.item [
        Timeline.marker [
            marker.isIcon
            color.isDanger
            prop.children [ Html.i [ prop.className "fas fa-trash" ] ]
        ]
        Timeline.content [
            Timeline.content.header <| TodoEvent.toString historyItem.Event
            Timeline.content.content (formatDate historyItem.At)
        ]
    ]

let timelineHeader =
    Timeline.header [
        Bulma.tag [ color.isPrimary; tag.isMedium; prop.text "History" ]
    ]

let eventReducer state history =
    match history.Event with
    | TodoCreated _ -> state @ [ createdSection history ]
    | TodoCompleted -> state @ [ completeSection history ]
    | TodoDeleted -> state @ [ deletedSection history ]

let timeline (events: TodoHistoryItem list) =
    Timeline.timeline (events |> List.fold eventReducer [ timelineHeader ])


let todoInfo (todo: Todo) (dispatch: Msg -> unit) =
    let createdDate = $"Created on %s{formatDate todo.Created}"

    Bulma.content [
        Bulma.label [ prop.text createdDate ]
        todoControls todo dispatch
    ]

let todoTitle (model: Model) =
    match model.Todo with
    | Loaded todo -> todo.Description
    | _ -> String.Empty

let loadingView =
    Bulma.column [ Bulma.progress [ color.isPrimary; prop.max 100 ] ]

let views (model: Model) (dispatch: Msg -> unit) =
    match model.Todo with
    | Loading
    | NotStarted -> loadingView
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
                    Bulma.box [ views model dispatch ]
                    Bulma.box [ timeline model.History ]
                ]
            ]
        ]
        confirmationModal model dispatch
    ]