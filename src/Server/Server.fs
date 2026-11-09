module Server

open System.Security.Claims
open System.Text.Json.Serialization
open Fable.Remoting.Server
open Fable.Remoting.Giraffe
open JasperFx
open JasperFx.Events.Projections
open JasperFx.MultiTenancy
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
open Giraffe
open Projections

exception ForbiddenException
exception NotFoundException

let requireRole (context: HttpContext) (role: string) =
    if not (context.User.IsInRole role) then
        raise ForbiddenException

let currentUserId (context: HttpContext) =
    context.User.FindFirst(ClaimTypes.NameIdentifier).Value

let todosApi (context: HttpContext) =
    let store = context.GetService<IDocumentStore>()
    let eventStore = EventStorage(store)
    let tenantId = currentUserId context

    {
        getTodos =
            fun () -> async {
                let! todos = eventStore.GetTodos(tenantId)
                return List.ofSeq todos
            }
        getTodo =
            fun id -> async {
                let! todo = eventStore.GetTodo(tenantId, id)

                if isNull (box todo) then
                    return Error NotFound
                else
                    return Ok todo
            }
        getHistory =
            fun id -> async {
                let! history = eventStore.GetHistory(tenantId, id)

                if isNull (box history) then
                    return []
                else
                    return history.Items
            }
        addTodo =
            fun todo -> async {
                requireRole context "todo-edit"

                let event = {
                    Id = todo.Id
                    Description = todo.Description
                }

                do! eventStore.AddTodo(tenantId, event)
                return todo
            }
        removeTodo =
            fun id -> async {
                requireRole context "todo-remove"

                let! existing = eventStore.GetTodo(tenantId, id)

                if isNull (box existing) then
                    raise NotFoundException

                do! eventStore.RemoveTodo(tenantId, id)
            }
        completeTodo =
            fun id -> async {
                requireRole context "todo-edit"

                let! existing = eventStore.GetTodo(tenantId, id)

                if isNull (box existing) then
                    return Error NotFound
                else
                    do! eventStore.CompleteTodo(tenantId, id)

                    let! todo = eventStore.GetTodo(tenantId, id)

                    return Ok todo
            }
    }

let onlyLoggedIn = pipeline {
    requires_authentication  (Auth.challenge JwtBearerDefaults.AuthenticationScheme)
}

let errorHandler (ex: exn) (routeInfo: RouteInfo<HttpContext>) =
    match ex with
    | ForbiddenException ->
        routeInfo.httpContext.Response.StatusCode <- 403
        Ignore
    | NotFoundException ->
        routeInfo.httpContext.Response.StatusCode <- 404
        Ignore
    | _ ->
        eprintfn "DEBUG ERROR HANDLER: %s" (ex.ToString())
        Propagate ex.Message

let todosRouter =
    Remoting.createApi ()
    |> Remoting.withRouteBuilder Route.builder
    |> Remoting.withErrorHandler errorHandler
    |> Remoting.fromContext todosApi
    |> Remoting.buildHttpHandler

let authRouter = router {
    pipe_through onlyLoggedIn

    forward "" todosRouter
}

let webApp = choose [
    authRouter
]

let marten (services: IServiceCollection) =
    services.AddMarten(fun (options: StoreOptions) ->
        let config =
            services.BuildServiceProvider().GetService<IConfiguration>()

        options.Connection(config.GetConnectionString "Db")

        options.AutoCreateSchemaObjects <- AutoCreate.CreateOrUpdate

        options.Events.TenancyStyle <- TenancyStyle.Conjoined

        options.Projections.Add(TodoProjection(), ProjectionLifecycle.Inline)
        options.Projections.Add(TodoHistoryProjection(), ProjectionLifecycle.Live)

        options.Schema.For<Todo>().MultiTenanted() |> ignore

        options.UseSystemTextJsonForSerialization(configure = fun v -> v.Converters.Add(JsonFSharpConverter())))
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

            tokenParameters.NameClaimType <- ClaimTypes.Name
            tokenParameters.RoleClaimType <- ClaimTypes.Role
            tokenParameters.ValidIssuer <- validIssuer
            tokenParameters.ValidateIssuerSigningKey <- true
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
    use_router webApp
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