module Server

open System
open Fable.Remoting.Server
open Fable.Remoting.Giraffe
open Saturn
open FSharp.Configuration

open Shared
open EventStore

type Settings = YamlConfig<"Config.yaml">

let todosApi =
    let config = Settings()
    let connection = config.DB.Connection
    let eventStore = EventStorage(connection)

    { getTodos = fun () -> async {
                            let! todos =  eventStore.GetTodos()
                            return List.ofSeq todos}
      addTodo =
        fun todo ->
            async {
                let event = { Id = todo.Id; Description = todo.Description }
                do eventStore.AddTodo event
                return todo
            }
      getTodo = fun id ->
                    async {
                        let! todo = eventStore.GetTodo id

                        return Ok todo
                    }
      getHistory =
          fun id ->
              async {
                  let! history = eventStore.GetHistory id
                  return history.Items
              }
      removeTodo = fun id ->
                    async {
                        return eventStore.RemoveTodo id
                    }
      completeTodo = fun id ->
                        async {
                            do eventStore.CompleteTodo id

                            let! todo = eventStore.GetTodo id

                            return Ok todo
                        }}

let webApp =
    Remoting.createApi ()
    |> Remoting.withRouteBuilder Route.builder
    |> Remoting.fromValue todosApi
    |> Remoting.buildHttpHandler

let app =
    application {
        use_router webApp
        memory_cache
        use_static "public"
        use_gzip
    }

[<EntryPoint>]
let main _ =
    run app
    0