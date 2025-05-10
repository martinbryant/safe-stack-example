module Session

type LoggedInUser = {
    Name: string
}

type User =
    | Guest
    | LoggedIn of LoggedInUser

