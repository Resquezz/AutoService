# AutoService Event Sourcing Lab

This project implements an event-sourced automotive service workflow with CQRS, RabbitMQ messaging, and a React UI.

## Architecture

- Command API: AutoService.Api
- Read model API: AutoService.ReadModel.Api
- Event consumer: AutoService.Consumer
- Audit consumer: AutoService.AuditConsumer
- React dashboard: autoservice-client
- Tests: AutoService.Tests

## Runtime dependencies

- PostgreSQL
- RabbitMQ

## Quick start

1. Start infrastructure:
   - PowerShell: `./scripts/start-infra.ps1`
   - or `docker compose up -d postgres rabbitmq`
2. Run the API:
   - `dotnet run --project AutoService.Api`
3. Run the consumer:
   - `dotnet run --project AutoService.Consumer`
4. Run the audit consumer:
   - `dotnet run --project AutoService.AuditConsumer`
5. Run the read model API:
   - `dotnet run --project AutoService.ReadModel.Api`
6. Run the frontend:
   - `cd autoservice-client && npm install && npm run dev -- --host 0.0.0.0`

## Default ports

- API: http://localhost:5284
- Read model API: http://localhost:5183
- RabbitMQ: http://localhost:15672
- React UI: http://localhost:5173

## Notes

- The frontend is configured to call the command API on port 5284 and the read model API on port 5183.
- If Docker Desktop is unavailable in the current environment, start PostgreSQL and RabbitMQ manually or from another local runtime.
