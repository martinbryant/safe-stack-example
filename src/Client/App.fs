module App

open Elmish
open Elmish.React
open Elmish.UrlParser
open Elmish.Navigation
open Urls
#if DEBUG
open Elmish.Debug
open Elmish.HMR
#endif

let pageParser : Parser<Url->Url, Url> =
  oneOf
    [
      map Url.TodoList top
      map Url.TodoList (s Url.TodoList.asString)
      map Url.Todo (s "todo" </> i32)
    ]

let urlUpdate (result:Option<Url>) model =
  match result with
  | Some url ->
      Index.initFromUrl url
  | None ->
      model, Navigation.newUrl Url.NotFound.asString

Program.mkProgram Index.init Index.update Index.view
|> Program.toNavigable (parsePath pageParser) urlUpdate
#if DEBUG
|> Program.withConsoleTrace
#endif
|> Program.withReactSynchronous "elmish-app"
#if DEBUG
|> Program.withDebugger
#endif
|> Program.run