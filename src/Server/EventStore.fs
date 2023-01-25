module EventStore

open CosmoStore
open CosmoStore.LiteDb
open System.IO
open System
open Shared
open Microsoft.FSharp.Reflection
open Shared.Todo

type TodoData = {
    Id: Guid
    Description: string option
}

type TodoEvent =
    | Created
    | Completed
    | Deleted

let fromString (s: string) =
    let caseInfo =
        FSharpType.GetUnionCases typeof<TodoEvent>
        |> Array.tryFind (fun case -> case.Name.ToLower() = s.ToLower())

    match caseInfo with
    | Some case ->
        match case.GetFields() with
        | [||] ->
            FSharpValue.MakeUnion(case, [||]) :?> TodoEvent |> Some
        | _ ->
            FSharpValue.MakeUnion(case,  [| "" |> box |] ) :?> TodoEvent |> Some
    | _ -> None

type EventStore() =

    let config : Configuration = {
        DBMode = Exclusive
        Folder = "data"
        StoreType = LocalDB
        Name = "TodoEvent.db"
    }

    [<Literal>]
    let streamId = "todo-events"

    let eventStore =
        config |> EventStore.getEventStore

    let appendEvent = eventStore.AppendEvent streamId

    let stateFolder (state: Todo) (event: EventRead<TodoData, int64>): Todo =
        match fromString event.Name with
        | Some case ->
            match case with
            | Created ->
                let todo = {
                    Id = Option.defaultValue (Guid.NewGuid()) event.CorrelationId
                    Description = event.Data.Description |> Option.defaultValue ""
                    Created = Some event.CreatedUtc
                    Completed = false
                    Deleted = false
                }
                todo

            | Completed ->
                { state with Completed = true }

            | Deleted ->
                { state with Deleted = true }
        | None -> failwith ""

    let optionStateFolder (state: Todo option) (events: EventRead<TodoData, int64> list): Todo option =
        match state with
        | Some todo -> Some <| List.fold stateFolder todo events
        | None -> None

    member _.GetTodo (id: Guid) =
        task {
            let! events = eventStore.GetEventsByCorrelationId id

            return match events with
                    | [] -> Error NotFound
                    | _ -> Ok <| List.fold stateFolder defaultTodo events
        }
        |> Async.AwaitTask

    member _.AddTodo (data: TodoData) =
        let event = {
            Id = Guid.NewGuid()
            CorrelationId = Some data.Id
            CausationId = None
            Name = nameof(Created)
            Data = data
            Metadata = None
        }

        appendEvent NoStream event
            |> Async.AwaitTask

    member _.CompleteTodo (id: Guid) =
        let event = {
            Id = Guid.NewGuid()
            CorrelationId = Some id
            CausationId = None
            Name = nameof(Completed)
            Data = { Id = id; Description = None }
            Metadata = None
        }

        eventStore.AppendEvent streamId Any event
            |> Async.AwaitTask

    member _.DeleteTodo (id: Guid) =
        let event = {
            Id = Guid.NewGuid()
            CorrelationId = None
            CausationId = None
            Name = (nameof(Deleted))
            Data = { Id = id; Description = None }
            Metadata = None
        }

        appendEvent Any event
            |> Async.AwaitTask

    member _.GetTodos () =
        task {
            let! events = eventStore.GetEvents streamId AllEvents

            return events
                    |> List.groupBy (fun event -> event.CorrelationId)
                    |> List.map (fun (_, list) -> List.fold stateFolder defaultTodo list)
        }
        |> Async.AwaitTask


