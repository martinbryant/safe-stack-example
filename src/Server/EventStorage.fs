module EventStore

open JasperFx.CodeGeneration
open Marten
open System
open Shared
open Weasel.Core
open Marten.Events.Projections
open System.Text.Json.Serialization
open Marten.Services

type EventStorage(connection: string) =

    let store = DocumentStore.For(fun options ->
        options.Connection(connection)

        options.GeneratedCodeMode <- TypeLoadMode.Auto
        options.AutoCreateSchemaObjects <- AutoCreate.All

        options.Projections.Snapshot<Todo> SnapshotLifecycle.Inline
        |> ignore
        options.Projections.LiveStreamAggregation<TodoHistory>
        |> ignore

        let serializer = SystemTextJsonSerializer ()
        serializer.Customize (fun v -> v.Converters.Add (JsonFSharpConverter ()))
        options.Serializer serializer
    )

    member _.AddTodo(data: CreatedData) =
        task {
            use session = store.LightweightSession()

            let event = TodoCreated data
            let todoId = data.Id

            session.Events.Append(todoId, event) |> ignore

            do! session.SaveChangesAsync()
        } |> Async.AwaitTask

    member _.CompleteTodo(id: Guid) =
        task {
            use session = store.LightweightSession()

            let event = TodoCompleted
            let todoId = id

            session.Events.Append(todoId, event) |> ignore

            do! session.SaveChangesAsync()
        } |> Async.AwaitTask

    member _.RemoveTodo(id: Guid) =
        task {
            use session = store.LightweightSession()

            let event = TodoDeleted
            let todoId = id

            session.Events.Append(todoId, event) |> ignore

            do! session.SaveChangesAsync()
        } |> Async.AwaitTask

    member _.GetTodo(id: Guid) =
        task {
            use session = store.LightweightSession()

            return! session.Query<Todo>()
                            .FirstOrDefaultAsync(fun todo -> todo.Id = id)
        } |> Async.AwaitTask

    member _.GetTodos() =
        task {
            use session = store.LightweightSession()

            return! session.Query<Todo>().ToListAsync()
        } |> Async.AwaitTask

    member _.GetHistory(id: Guid) =
        task {
            use session = store.LightweightSession()

            let events = session.Events.AggregateStream<TodoHistory> id

            return events
        } |> Async.AwaitTask
