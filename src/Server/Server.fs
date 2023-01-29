module Server

open Fable.Remoting.Server
open Fable.Remoting.Giraffe
open Saturn

open Shared
open LiteDB.FSharp
open LiteDB
open Shared
open EventStore
open System.IO

type Storage() as storage =
    let current = Directory.GetCurrentDirectory()
    let directory = Path.Join(current, "/data")
    do
        if(Directory.Exists(directory) = false) then
            Directory.CreateDirectory(directory) |> ignore

    let filename = Path.Join(directory, "/Todo.db")
    let database =
        let mapper = FSharpBsonMapper()
        let connStr = sprintf "Filename=%s;mode=Exclusive" filename
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
        | null -> Error <| NotFound
        | _ -> Ok result

    member _.CompleteTodo (id: int) =
        let identifier = BsonValue(id)
        let findResult = todos.FindById(identifier)
        let result = match box findResult with
                        | null -> Error <| NotFound
                        | _ -> Ok findResult

        Result.map Todo.complete result
            |> Result.bind (fun todo ->
                                if todos.Update todo
                                    then
                                        Ok todo
                                    else
                                        Error <| Request "Failed to update database")

    member _.RemoveTodo (id: int) =
        let identifier = BsonValue(id)
        todos.Delete(identifier) |> ignore

let todosApi =
    let storage = Storage()
    let eventStore = EventStorage()

    { getTodos = fun () -> async { return storage.GetTodos() }
      addTodo =
        fun todo ->
            async {
                let event = { Id = todo.Id; Description = todo.Description }
                do eventStore.AddTodo event
                return todo
            }
      getTodo = fun id ->
                    async {
                        return
                            storage.GetTodo id
                    }
      removeTodo = fun id ->
                    async {
                        do storage.RemoveTodo id
                    }
      completeTodo = fun id ->
                        async {
                            return storage.CompleteTodo id
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