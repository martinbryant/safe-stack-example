module Session

type Session = {
    Name: string
}

type User =
    | Guest
    | LoggedIn of Session

