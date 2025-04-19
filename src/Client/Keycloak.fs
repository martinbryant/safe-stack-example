module Keycloak

open Browser
open Fable.Core.JS
open Fable.Core.JsInterop

let windowLocation = window.location.origin


type KeycloakConfig = {
    url: string
    realm: string
    clientId: string
}

type TokenParsed = {
    given_name: string
}

type KeycloakInitOptions = {
    onLoad: string
    silentCheckSsoRedirectUri: string
    enableLogging: bool
    responseMode: string
}

type KeycloakLoginOptions = {
    redirectUri: string
}

type Keycloak =
    abstract didInitialize: bool
    abstract authenticated: bool
    abstract tokenParsed: TokenParsed
    abstract token: string
    abstract init: options: KeycloakInitOptions -> Promise<bool>
    abstract login: options: KeycloakLoginOptions -> Promise<unit>

let keycloakInstance = importDefault<JsConstructor> "keycloak-js"

let create (config: KeycloakConfig): Keycloak  = unbox <| keycloakInstance.Create [|box config|]