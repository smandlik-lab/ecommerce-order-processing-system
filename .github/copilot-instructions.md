# Project Guidelines

## Project
- This repository is an ASP.NET Core Web API targeting .NET 10 (`net10.0`), with nullable reference types and implicit usings enabled.
- The API uses Entity Framework Core with SQL Server, JWT bearer authentication, and AWS SQS for asynchronous order processing.
- Keep changes within the existing structure: `Controllers`, `DTOs`, `Data`, `Models`, `Repositories`, and `Services`.

## Implementation Conventions
- Keep controllers focused on HTTP concerns; put business logic in services and data access in repositories/`AppDbContext`.
- Register application services and dependencies in `Program.cs`, following the existing interface/implementation pattern.
- Use the existing DTOs for API inputs, outputs, and order queue messages; avoid exposing EF entities directly from API endpoints.
- Preserve asynchronous I/O and pass cancellation tokens where the relevant API supports them.
- When changing the EF model, update it through EF Core migrations and keep the model snapshot in sync; do not edit generated migration files by hand unless correcting generated migration output.
- Keep the API contract and the `EcommerceOrderProcessing.http` requests aligned when changing endpoints.

## Design Patterns in Use
- **Layered architecture:** API controllers delegate to services, which use repositories and `AppDbContext` for persistence. Keep responsibilities in the existing layer rather than moving business or data-access logic into controllers.
- **Repository pattern:** `GenericRepository<T>` provides common EF Core operations, with specialized repositories such as `OrderRepository` for aggregate-specific queries. Extend the specialized repository/interface when a query is specific to an entity or use case.
- **Unit of Work:** `UnitOfWork` coordinates persistence through `AppDbContext.SaveChangesAsync()`. Use the established repository/unit-of-work abstractions for related order changes that should be saved together.
- **Dependency injection:** Services and repositories are registered in `Program.cs` and depend on interfaces where the project provides them. Follow this registration and constructor-injection approach for new components.
- **Data Transfer Objects (DTOs):** Request, response, and queue-message DTOs separate external data contracts from EF Core entities. Keep mapping at the service/API boundary consistent with existing code.
- **Producer-consumer / asynchronous messaging:** `OrderQueueService` publishes order messages to AWS SQS and `OrderQueueConsumerService` consumes them as a hosted background service. Keep queued messages explicit and small, and preserve the producer/consumer contract when changing order processing.

## Configuration and Secrets
- Keep environment-specific values in configuration, but never add credentials, signing keys, or other secrets to tracked files.
- For local JWT development, use .NET User Secrets as documented in `README.md`. Configure AWS credentials through the AWS SDK's standard credential chain.
- Preserve required configuration keys such as `ConnectionStrings:DefaultConnection`, `Jwt:Key`, and `AWSQueueSettings:OrderQueueUrl`; fail clearly when a required setting is absent.

## AI Guardrails (Don'ts)
- Do not invent API routes, DTO fields, entity properties, service methods, configuration keys, package versions, or architectural components. Inspect the relevant controller, interface, implementation, model, and configuration before using or changing them.
- Do not assume a feature exists because it is mentioned in `README.md`, appears in a DTO/model, or is listed as a future improvement. Confirm it is wired into the running application by checking its implementation and registrations.
- Do not claim an endpoint, integration, test, build, migration, or other behavior works unless the repository contains evidence and, where applicable, it has been run and verified.
- Do not assume there are no tests: the solution includes `EcommerceOrderProcessing.Tests`. Check existing tests and conventions before adding or changing tests.
- Do not add a new project, package, architectural pattern, folder hierarchy, or abstraction unless the requested change needs it and the existing approach cannot reasonably support it.
- Do not put credentials or local machine settings in tracked configuration, and do not run database migrations against a configured database unless explicitly required by the task.
- Do not make unrelated cleanup, broad refactors, or API/database contract changes while implementing a focused request. Preserve existing behavior unless the requested change requires otherwise.

## Task Structure for AI Changes
1. Inspect the relevant source, tests, and configuration; treat checked-in code and project files as evidence, not assumptions.
2. Summarize the verified behavior and identify any unknowns that affect the requested change. Ask for clarification rather than inventing product behavior when the ambiguity materially changes the implementation.
3. Make the smallest complete change that follows the existing architecture and preserves unrelated behavior. Update directly related tests and documentation.
4. Run the narrowest relevant validation command. If validation cannot be run, state that clearly and do not imply it passed.
5. Report the files changed, the behavior changed, validation performed and its result, plus any remaining assumptions or limitations.

## Build and Run
- Restore and build from the repository root:
  - `dotnet restore EcommerceOrderProcessing.slnx`
  - `dotnet build EcommerceOrderProcessing.slnx`
- Run the API with `dotnet run --project EcommerceOrderProcessing.csproj`.
- Run the test project with `dotnet test EcommerceOrderProcessing.Tests/EcommerceOrderProcessing.Tests.csproj`.
- Add a migration with `dotnet ef migrations add <MigrationName>` and apply it with `dotnet ef database update`.
- Add or update tests for changed behavior, following the existing xUnit test project and conventions.
