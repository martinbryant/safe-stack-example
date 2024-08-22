module Server.Tests

open Expecto

open Shared
open Server

let server =
    testList "Server" [
        testCase "Adding valid Todo"
        <| fun _ ->
            let validTodo = Todo.create "TODO"
            let expectedResult = Ok validTodo

            let storage = new Storage()

            let result = storage.AddTodo validTodo

            let todos = storage.GetTodos()

            Expect.equal result expectedResult "Result should be ok"
            Expect.contains todos validTodo "Storage should contain new todo"
    ]

let all = testList "All" [ Shared.Tests.shared; server ]

[<EntryPoint>]
let main _ = runTestsWithCLIArgs [] [||] all