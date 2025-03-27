module EventStore

open Marten
open System
open Shared

type EventStorage(store: IDocumentStore) =
    member _.AddTodo(data: CreatedData) =
        task {
            use session = store.LightweightSession()

            let event = TodoCreated data
            let todoId = data.Id

            session.Events.Append(todoId, event) |> ignore

            do! session.SaveChangesAsync()
        }
        |> Async.AwaitTask

    member _.CompleteTodo(id: Guid) =
        task {
            use session = store.LightweightSession()

            let event = TodoCompleted
            let todoId = id

            session.Events.Append(todoId, event) |> ignore

            do! session.SaveChangesAsync()
        }
        |> Async.AwaitTask

    member _.RemoveTodo(id: Guid) =
        task {
            use session = store.LightweightSession()

            let event = TodoDeleted
            let todoId = id

            session.Events.Append(todoId, event) |> ignore

            do! session.SaveChangesAsync()
        }
        |> Async.AwaitTask

    member _.GetTodo(id: Guid) =
        task {
            use session = store.LightweightSession()

            return!
                session
                    .Query<Todo>()
                    .FirstOrDefaultAsync(fun todo -> todo.Id = id)
        }
        |> Async.AwaitTask

    member _.GetTodos() =
        task {
            use session = store.LightweightSession()

            return! session.Query<Todo>().ToListAsync()
        }
        |> Async.AwaitTask

    member _.GetHistory(id: Guid) =
        task {
            use session = store.LightweightSession()

            let! events = session.Events.AggregateStreamAsync<TodoHistory> id

            return events
        }
        |> Async.AwaitTask