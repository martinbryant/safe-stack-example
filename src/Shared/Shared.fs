namespace Shared

open System

type CreatedData = { Id: Guid; Description: string }

type TodoEvent =
    | TodoCreated of CreatedData
    | TodoCompleted
    | TodoDeleted

[<CLIMutable>]
type TodoHistoryItem = { At: DateTimeOffset; Event: TodoEvent }

[<CLIMutable>]
type TodoHistory = { Id: Guid; Items: TodoHistoryItem list }

[<CLIMutable>]
type Todo = {
    Id: Guid
    Description: string
    Created: DateTimeOffset
    Completed: DateTimeOffset option
    Deleted: DateTimeOffset option
}

module Todo =
    let isValid (description: string) =
        String.IsNullOrWhiteSpace description |> not

    let create (description: string) = {
        Id = Guid.NewGuid()
        Description = description
        Created = DateTimeOffset.UtcNow
        Completed = None
        Deleted = None
    }

    let complete (todo: Todo) = { todo with Completed = None }

module TodoEvent =
    let toString event =
        match event with
        | TodoCreated _ -> "Created"
        | TodoCompleted -> "Completed"
        | TodoDeleted -> "Deleted"

module Route =
    let builder typeName methodName =
        $"/api/%s{typeName}/%s{methodName}"


type AppError =
    | NotFound
    | Request of string

type ITodosApi = {
    getTodos: unit -> Async<Todo list>
    getTodo: Guid -> Async<Result<Todo, AppError>>
    getHistory: Guid -> Async<TodoHistoryItem list>
    addTodo: Todo -> Async<Todo>
    removeTodo: Guid -> Async<unit>
    completeTodo: Guid -> Async<Result<Todo, AppError>>
}