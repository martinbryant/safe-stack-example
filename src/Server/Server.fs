module Server

open System.Text.Json.Serialization
open Fable.Remoting.Server
open Fable.Remoting.Giraffe
open Marten.Events.Projections
open Marten.Services
open Microsoft.AspNetCore.Authentication.JwtBearer
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Http
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.DependencyInjection
open Microsoft.IdentityModel.Tokens
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

let onlyLoggedIn = pipeline {
    requires_authentication  (Giraffe.Auth.challenge JwtBearerDefaults.AuthenticationScheme)
}

let webApp =
    Remoting.createApi ()
    |> Remoting.withRouteBuilder Route.builder
    |> Remoting.fromContext todosApi
    |> Remoting.buildHttpHandler

let authRouter = router {
    pipe_through onlyLoggedIn

    forward "" webApp
}

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

let jwt (services: IServiceCollection) =
    let config =
            services.BuildServiceProvider().GetService<IConfiguration>()
    let validIssuer = config["Keycloak:ValidIssuer"]
    let metadata = config["Keycloak:Metadata"]

    services
        .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(fun (options: JwtBearerOptions) ->
            let tokenParameters = TokenValidationParameters()

            // tokenParameters.NameClaimType <- ClaimTypes.Name
            // tokenParameters.RoleClaimType <- ClaimTypes.Role
            tokenParameters.ValidIssuer <- validIssuer
            // tokenParameters.ValidateIssuerSigningKey <- true
            tokenParameters.ValidAudience <- "account"

            options.Audience <- "account"
            options.MetadataAddress <- metadata
            options.RequireHttpsMetadata <- false
            options.TokenValidationParameters <- tokenParameters
            ) |> ignore

    services

let configureServices = marten >> jwt

let configureApp (builder: IApplicationBuilder) =
    builder.UseAuthentication()

let app = application {
    use_router authRouter
    app_config configureApp
    service_config configureServices
    memory_cache
    use_static "public"
    use_gzip
}

[<EntryPoint>]
let main _ =
    run app
    0