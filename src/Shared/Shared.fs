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
        Completed: DateTimeOffset option
        Deleted: DateTimeOffset option }

    member this.Apply(event: TodoEvent, meta: IEvent) : Todo =
        match event with
        | TodoCreated data ->
            {
                Id = data.Id
                Description = data.Description
                Created = meta.Timestamp
                Completed = None
                Deleted = None }
        | TodoCompleted ->
            { this with Completed = Some meta.Timestamp }
        | TodoDeleted ->
            { this with Deleted = Some meta.Timestamp }

module Todo =
    let isValid (description: string) =
        String.IsNullOrWhiteSpace description |> not

    let create (description: string) =
        { Id = Guid.NewGuid()
          Description = description
          Created = DateTimeOffset.UtcNow
          Completed = None
          Deleted = None }

    let complete (todo: Todo) =
        { todo with Completed = None }

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