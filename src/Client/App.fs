module App

open Elmish
open Elmish.React
#if DEBUG
open Elmish.HMR
#endif
open Fable.Core.JsInterop

importSideEffects "./style.scss"

Program.mkProgram Index.init Index.update Index.view
#if DEBUG
|> Program.withConsoleTrace
#endif
|> Program.withReactSynchronous "elmish-app"
#if DEBUG
|> Program.withConsoleTrace
#endif
|> Program.run