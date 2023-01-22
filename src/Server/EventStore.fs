module EventStore

open CosmoStore
open CosmoStore.LiteDb
open System.IO
open Shared
open System
open LiteDB

type CreatedTodoData = {
    Description: string;
}

type TodoEvent =
    | Created of CreatedTodoData
    | Completed
    | Deleted

type EventStore() =
    let current = Directory.GetCurrentDirectory()

    let directory = Path.Join(current, "/data")

    do
        if(Directory.Exists(directory) = false) then
            Directory.CreateDirectory(directory) |> ignore

    let config : Configuration = {
        DBMode = Exclusive
        Folder = "data"
        StoreType = LocalDB
        Name = "TodoEvent.db"
    }

    let streamId = "todo-events"

    let eventStore =
        config |> EventStore.getEventStore

    let appendEvent = eventStore.AppendEvent streamId

    member _.AddTodo (data: CreatedTodoData) =
        let event = {
            Id = Guid.NewGuid()
            CorrelationId = None
            CausationId = None
            Name = nameof(Created)
            Data = data
            Metadata = None
        }

        appendEvent NoStream event
            |> Async.AwaitTask

    member _.GetEvents () =
        eventStore.GetEvents streamId AllEvents
            |> Async.AwaitTask

    // member _.CompleteTodo () =
    //     let event: EventWrite<BsonValue> = {
    //         Id = Guid.NewGuid()
    //         CorrelationId = None
    //         CausationId = None
    //         Name = (nameof(Completed))
    //         Data = BsonValue()
    //         Metadata = None
    //     }

    //     appendEvent Any event
    //         |> Async.AwaitTask

    // member _.DeleteTodo () =
    //     let event: EventWrite<BsonValue> = {
    //         Id = Guid.NewGuid()
    //         CorrelationId = None
    //         CausationId = None
    //         Name = (nameof(Deleted))
    //         Data = BsonValue()
    //         Metadata = None
    //     }

    //     appendEvent Any event
    //         |> Async.AwaitTask
    //         |> Async.RunSynchronously



