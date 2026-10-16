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

## Build and Run
- Restore and build from the repository root:
  - `dotnet restore EcommerceOrderProcessing.slnx`
  - `dotnet build EcommerceOrderProcessing.slnx`
- Run the API with `dotnet run --project EcommerceOrderProcessing.csproj`.
- Add a migration with `dotnet ef migrations add <MigrationName>` and apply it with `dotnet ef database update`.
- There is currently no test project in the repository. If one is added, include tests for changed behavior and run its tests before completion.
