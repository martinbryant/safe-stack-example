module EventStore

open Marten
open System
open Marten.Events.Projections
open Shared
// open Weasel.Core

type EventStorage() =

    let store = DocumentStore.For("host=localhost;port=9001;database=event_store;password=Monkey1234;username=postgres")

    member _.AddTodo(event: CreatedData) =
        use session = store.OpenSession()

        let todoId = Guid.NewGuid()

        session.Events.Append(todoId, event) |> ignore

        session.SaveChanges()
