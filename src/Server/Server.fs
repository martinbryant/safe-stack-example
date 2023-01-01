module Server

open Fable.Remoting.Server
open Fable.Remoting.Giraffe
open Saturn

open Shared
open LiteDB.FSharp
open LiteDB

type Storage() as storage =
    let database =
        let mapper = FSharpBsonMapper()
        let connStr = "Filename=Todo.db;mode=Exclusive"
        new LiteDatabase (connStr, mapper)

    let todos = database.GetCollection<Todo> "todos"

    do
        if storage.GetTodos() |> Seq.isEmpty then
            storage.AddTodo(Todo.create "Create new SAFE project") |> ignore
            storage.AddTodo(Todo.create "Write your app") |> ignore
            storage.AddTodo(Todo.create "Ship it !!!") |> ignore

    member _.GetTodos () =
        todos.FindAll () |> List.ofSeq

    member _.AddTodo (todo:Todo) =
        if Todo.isValid todo.Description then
            let id = (todos.Insert todo).AsInt32
            let newTodo = { todo with Id = id }
            Ok newTodo
        else
            Error "Invalid todo"

    member _.GetTodo (id: int) =
        let identifier = BsonValue(id)
        let result = todos.FindById(identifier)
        match box result with
        | null -> Error "Todo not found"
        | _ -> Ok result

let todosApi =
    let storage = Storage()

    { getTodos = fun () -> async { return storage.GetTodos() }
      addTodo =
        fun todo ->
            async {
                return
                    match storage.AddTodo todo with
                    | Ok newTodo -> newTodo
                    | Error e -> failwith e
            }
      getTodo = fun id ->
                    async {
                        return
                            match storage.GetTodo id with
                            | Ok todo -> todo
                            | Error e -> failwith e
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