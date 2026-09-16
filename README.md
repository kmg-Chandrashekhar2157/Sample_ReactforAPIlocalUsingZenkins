# Poc.Api — .NET Backend

ASP.NET Core Web API (.NET 10) exposing a small Todo REST API. This is the **backend**
project; it has no knowledge of React beyond the CORS origins it allows.

## Run in Visual Studio

1. Open `..\ReactDotNetPoc.sln` (in the POC root) — or just double-click it in Explorer.
2. `Poc.Api` is the only project, so it is already the startup project. If Visual Studio
   ever loses that, right-click it in Solution Explorer → **Set as Startup Project**.
3. Pick a launch profile from the dropdown next to the green ▶ Run button:
   - **http** — <http://localhost:5147>, no certificate needed. Start here.
   - **https** — also serves <https://localhost:7032>. Run
     `dotnet dev-certs https --trust` once first, or the browser warns.
   - **http (no browser)** — same as `http` but doesn't open a browser tab. Handy when
     the React app is your UI.
4. Press **F5** (debug) or **Ctrl+F5** (run without debugging).

F5 opens a browser on `/api/todos`, so you should immediately see the seeded todos as
JSON. To try `POST` / `PUT` / `DELETE`, open `Poc.Api.http` in Visual Studio (2022
17.8+) and click **Send request** above any block.

Breakpoints work as usual — put one in `TodosController.GetAll` and refresh the browser
or the React page.

## Run from the command line

```powershell
dotnet run --launch-profile http
```

Listens on <http://localhost:5147>. Use `--launch-profile https` for
<https://localhost:7032> as well (run `dotnet dev-certs https --trust` once first).

## Endpoints

| Method | Route             | Description                      | Success |
| ------ | ----------------- | -------------------------------- | ------- |
| GET    | `/api/health`     | Liveness probe                   | 200     |
| GET    | `/api/todos`      | List todos                       | 200     |
| GET    | `/api/todos/{id}` | Get one todo                     | 200     |
| POST   | `/api/todos`      | Create `{ "title": "…" }`        | 201     |
| PUT    | `/api/todos/{id}` | Replace `{ title, isComplete }`  | 200     |
| DELETE | `/api/todos/{id}` | Delete a todo                    | 204     |

Missing ids return `404`; an empty or over-long title returns `400` with an ASP.NET
validation `ProblemDetails` body.

The OpenAPI document is served in Development at
<http://localhost:5147/openapi/v1.json>. `Poc.Api.http` has ready-to-send requests for
the VS Code REST Client / Visual Studio HTTP editor.

## Layout

| Path                            | Purpose                                              |
| ------------------------------- | ---------------------------------------------------- |
| `Program.cs`                    | DI, CORS policy, pipeline, `/api/health`             |
| `Controllers/TodosController.cs`| REST endpoints                                       |
| `Models/`                       | `TodoItem` plus request DTOs with validation         |
| `Services/ITodoStore.cs`        | Storage abstraction                                  |
| `Services/InMemoryTodoStore.cs` | Singleton in-memory implementation, seeded at startup|

## CORS

`Program.cs` reads allowed origins from `Cors:AllowedOrigins`
(`appsettings.Development.json`), defaulting to `http://localhost:5173` — the Vite dev
server. Add an origin there if you run the frontend on a different port.

## Notes for turning this into a real service

- Data lives in process memory and resets on restart. Replace `InMemoryTodoStore` with an
  EF Core-backed `ITodoStore` — nothing in the controller has to change.
- There is no authentication. Add JWT bearer auth and `[Authorize]` when you need it.
- HTTPS redirection is enabled only outside Development, to keep the dev loop
  certificate-free.
