module EventStore

open CosmoStore
open CosmoStore.LiteDb
open System.IO
open System

type TodoData = {
    Id: Guid
    Description: string option
}

type TodoEvent =
    | Created
    | Completed
    | Deleted

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

    member _.GetEvents () =
        eventStore.GetEvents streamId AllEvents
            |> Async.AwaitTask


