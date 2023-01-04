namespace Shared

open System

[<CLIMutable>]
type Todo = { Id: int; Description: string; Created: DateTime }

module Todo =
    let isValid (description: string) =
        String.IsNullOrWhiteSpace description |> not

    let create (description: string) =
        { Id = 0
          Description = description
          Created = DateTime.UtcNow }

module Route =
    let builder typeName methodName =
        sprintf "/api/%s/%s" typeName methodName


type AppError =
    | NotFound
    | Request of string

type ITodosApi =
    { getTodos: unit -> Async<Todo list>
      addTodo: Todo -> Async<Todo>
      getTodo: int -> Async<Result<Todo, AppError>>
      removeTodo: int -> Async<unit> }