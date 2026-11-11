# CoffeeBar
Little social community Coffee App

Colleagues add the coffees and gelatos they had, pay their open amount and see
when they had how much on their member page.

## Structure

| Folder | Content |
| --- | --- |
| `CoffeeBar/` | Blazor Server app (.NET 10, EF Core with SQLite) |
| `CoffeeBar.Tests/` | xUnit tests for the services |
| `CoffeeBar.sln` | Solution with both projects |

## Commands

Run from the repository root:

```sh
dotnet build                      # build app and tests
dotnet test                       # run all tests
dotnet run --project CoffeeBar    # start the app on http://localhost:5138
```

The database `CoffeeBar/CoffeeBar_Context.db` is used by default.
After a model change add a migration with
`dotnet ef migrations add <Name> --project CoffeeBar` and apply it with
`dotnet ef database update --project CoffeeBar`.
