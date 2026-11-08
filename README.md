# SAFE Template

I have started this project to learn how to use the Safe Template and related frameworks by expanding on the base Todo application

The application has been deployed at

[Demo](https://safe.martinbryant.dev/)

Below I have listed the pull requests which

* [Add database](https://github.com/martinbryant/safe-stack-example/pull/1)
* [Add routing](https://github.com/martinbryant/safe-stack-example/pull/2)

## Authentication & permissions

Login is handled by Keycloak (see `docker-compose.yml`). Two realm roles gate the mutating todo actions:

* `todo-edit` — add and complete todos
* `todo-remove` — delete todos

`docker compose up` imports `./keycloak/realm-export.json` automatically (`--import-realm`), which creates the `safe-todo` realm, the `todo-client` client, the `todo-roles` client scope (maps realm roles onto a flat `roles` claim in the access token), the two roles above, and three local-dev test accounts:

| Username      | Password      | Roles                        |
|----------------|---------------|-------------------------------|
| `edit-user`    | `edit-user`   | `todo-edit`                   |
| `remove-user`  | `remove-user` | `todo-remove`                 |
| `full-user`    | `full-user`   | `todo-edit`, `todo-remove`    |

These credentials are for local development only — never reuse them anywhere else.

A user attempting an action their roles don't permit gets an HTTP 403 and a "You don't have permission to do this" toast (the same pattern used for a 401 when not logged in at all).

If the realm needs to be recreated by hand instead of via import, the role → claim mapping lives at **Client scopes → todo-roles → Mappers → realm roles** in the Keycloak admin console — it's a "User Realm Role" mapper with token claim name `roles`, multivalued, added to both the ID and access token.
