namespace Shared

open System

type AddTodo = {
    Id: int
    Description: string
}

type CompleteTodo = {
    Id: int
}

[<CLIMutable>]
type Todo = {
    Id: Guid
    Description: string
    Created: DateTime option
    Completed: bool
}

module Todo =
    let isValid (description: string) =
        String.IsNullOrWhiteSpace description |> not

    let create (description: string) =
        { Id = Guid.NewGuid ()
          Description = description
          Created = Some DateTime.UtcNow
          Completed = false }

    let complete (todo: Todo) =
        { todo with Completed = true }

module Route =
    let builder typeName methodName =
        sprintf "/api/%s/%s" typeName methodName


type AppError =
    | NotFound
    | Request of string

type ITodosApi =
    { getTodos: unit -> Async<Todo list>
      addTodo: Todo -> Async<Todo>
      getTodo: Guid -> Async<Result<Todo, AppError>>
      removeTodo: Guid -> Async<unit>
      completeTodo: Guid -> Async<Result<Todo, AppError>> }