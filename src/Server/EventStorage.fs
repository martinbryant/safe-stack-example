module EventStore

open Marten
open System
open Shared
open Weasel.Core
open Marten.Events.Projections
open System.Text.Json.Serialization
open Marten.Services
open LamarCodeGeneration

type EventStorage() =

    let store = DocumentStore.For(fun options ->
                    options.Connection("host=host.docker.internal;port=9501;database=todo_store;password=Monkey1234;username=postgres")

                    options.GeneratedCodeMode <- TypeLoadMode.Auto
                    options.AutoCreateSchemaObjects <- AutoCreate.All

                    options.Projections.SelfAggregate<Todo> (ProjectionLifecycle.Inline)
                    |> ignore

                    let serializer = SystemTextJsonSerializer ()
                    serializer.Customize (fun v -> v.Converters.Add (JsonFSharpConverter ()))
                    options.Serializer (serializer)
    )

    member _.AddTodo(data: CreatedData) =
        use session = store.OpenSession()

        let event = TodoCreated data
        let todoId = data.Id

        session.Events.Append(todoId, event) |> ignore

        session.SaveChanges()

    member _.CompleteTodo(id: Guid) =
        use session = store.OpenSession()

        let event = TodoCompleted
        let todoId = id

        session.Events.Append(todoId, event) |> ignore

        session.SaveChanges()

    member _.RemoveTodo(id: Guid) =
        use session = store.OpenSession()

        let event = TodoDeleted
        let todoId = id

        session.Events.Append(todoId, event) |> ignore

        session.SaveChanges()

    member _.GetTodo(id: Guid) =
        task {
            use! session = store.OpenSessionAsync(new SessionOptions())

            return! session.Query<Todo>()
                            .FirstOrDefaultAsync(fun todo -> todo.Id = id)
        } |> Async.AwaitTask

    member _.GetTodos() =
        task {
            use! session = store.OpenSessionAsync(new SessionOptions())

            return! session.Query<Todo>().ToListAsync()
        } |> Async.AwaitTask
