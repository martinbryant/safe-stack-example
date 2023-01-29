namespace Shared

open System
open Marten.Events

type CreatedData = {
    Id: Guid
    Description: string
}

type TodoEvent =
    | TodoCreated of CreatedData
    | TodoCompleted
    | TodoDeleted

[<CLIMutable>]
type Todo =
    {   Id: Guid
        Description: string
        Created: DateTimeOffset
        Completed: bool
        Deleted: bool }

    member this.Apply(event: TodoEvent, meta: IEvent) : Todo =
        match event with
        | TodoCreated data ->
            {
                Id = data.Id
                Description = data.Description
                Created = meta.Timestamp
                Completed = false
                Deleted = false }
        | TodoCompleted ->
            { this with Completed = true }
        | TodoDeleted ->
            { this with Deleted = true }

module Todo =
    let isValid (description: string) =
        String.IsNullOrWhiteSpace description |> not

    let create (description: string) =
        { Id = Guid.NewGuid()
          Description = description
          Created = DateTimeOffset.UtcNow
          Completed = false
          Deleted = false }

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