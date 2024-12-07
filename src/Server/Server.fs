module Server

open System.Text.Json.Serialization
open Fable.Remoting.Server
open Fable.Remoting.Giraffe
open Marten.Events.Projections
open Marten.Services
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.DependencyInjection
open Saturn
open Shared
open Marten
open EventStore
open Weasel.Core

let todosApi (context: HttpContext) =
    let store = context.GetService<IDocumentStore>()
    let eventStore = EventStorage(store)

    {
        getTodos =
            fun () -> async {
                let! todos = eventStore.GetTodos()
                return List.ofSeq todos
            }
        addTodo =
            fun todo -> async {
                let event = {
                    Id = todo.Id
                    Description = todo.Description
                }

                do! eventStore.AddTodo event
                return todo
            }
        getTodo =
            fun id -> async {
                let! todo = eventStore.GetTodo id

                return Ok todo
            }
        getHistory =
            fun id -> async {
                let! history = eventStore.GetHistory id
                return history.Items
            }
        removeTodo = eventStore.RemoveTodo
        completeTodo =
            fun id -> async {
                do! eventStore.CompleteTodo id

                let! todo = eventStore.GetTodo id

                return Ok todo
            }
    }

let webApp =
    Remoting.createApi ()
    |> Remoting.withRouteBuilder Route.builder
    |> Remoting.fromContext todosApi
    |> Remoting.buildHttpHandler

let marten (services: IServiceCollection) =
    services.AddMarten(fun (options: StoreOptions) ->
        let config =
            services.BuildServiceProvider().GetService<IConfiguration>()

        options.Connection(config.GetConnectionString "Db")

        options.AutoCreateSchemaObjects <- AutoCreate.CreateOrUpdate

        options.Projections.Snapshot<Todo> SnapshotLifecycle.Inline |> ignore
        options.Projections.LiveStreamAggregation<TodoHistory> |> ignore

        let serializer = SystemTextJsonSerializer()
        serializer.Configure(fun v -> v.Converters.Add(JsonFSharpConverter()))
        options.Serializer serializer)
    |> ignore

    services

let configureServices = marten

let app = application {
    use_router webApp
    service_config configureServices
    memory_cache
    use_static "public"
    use_gzip
}

[<EntryPoint>]
let main _ =
    run app
    0