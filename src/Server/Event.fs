module EventStore

open CosmoStore
open CosmoStore.LiteDb
open System.IO
open Shared
open System

type EventStore() =
    let current = Directory.GetCurrentDirectory()

    let directory = Path.Join(current, "/data")

    do
        if(Directory.Exists(directory) = false) then
            Directory.CreateDirectory(directory) |> ignore

    let config : Configuration = {
        DBMode = Exclusive
        Folder = directory
        StoreType = LocalDB
        Name = "TodoEvent.db"
    }

    let streamId = "todo-events"

    member _.AddTodo (todo: AddTodo) =
        let event: EventWrite<AddTodo> = {
            Id = Guid.NewGuid()
            CorrelationId = None
            CausationId = None
            Name = nameof(AddTodo)
            Data = todo
            Metadata = Some todo
        }

        let eventStore =
            config |> EventStore.getEventStore

        eventStore.AppendEvent streamId NoStream event

    member _.CompleteTodo (todo: CompleteTodo) =
        let event: EventWrite<CompleteTodo> = {
            Id = Guid.NewGuid()
            CorrelationId = None
            CausationId = None
            Name = ""
            Data = todo
            Metadata = Some todo
        }

        let eventStore =
            config |> EventStore.getEventStore

        eventStore.AppendEvent streamId Any event

