# FlexOMatic

FlexOMatic is an AI-assisted content pipeline for generating stylized crop concepts for **Flex Farm**, a Roblox-style game project.

The app takes a rough user idea, turns it into a structured image-generation prompt, generates a transparent low-poly crop image, stores the result, and makes it available through a lightweight web gallery.

## What it demonstrates

- ASP.NET Core / .NET 8 backend design
- REST API endpoints and dependency injection
- OpenAI-powered prompt transformation and image generation
- Swappable prompt-polishing providers
- Input guardrails and family-safe prompt handling
- Azure Blob Storage integration
- SQL Server persistence
- Request logging and per-IP submission limits
- Lightweight web frontend for browsing and submitting crop concepts

## High-level flow

1. A user submits a crop idea from the web interface.
2. The API validates the request and applies submission limits.
3. The prompt pipeline converts the raw idea into a structured, game-ready image prompt and a short crop name.
4. OpenAI image generation creates a transparent 1024×1024 crop asset.
5. The generated image is uploaded to Azure Blob Storage.
6. Crop metadata is stored in SQL Server.
7. The result is returned to the frontend and displayed in the gallery.

## Project structure

```text
FlexOMatic.API/
  Controllers/        REST endpoints for crop, image, and prompt operations
  DAL/                SQL Server persistence
  Interfaces/         Service abstractions
  Models/             Request and domain models
  Services/           OpenAI, prompt, and Azure Blob integrations

FlexOMatic.Web/
  wwwroot/             Static frontend assets
```

## Tech stack

- **C# / .NET 8**
- **ASP.NET Core Web API**
- **OpenAI APIs**
- **Azure Blob Storage**
- **SQL Server**
- **HTML / JavaScript / Tailwind CSS**
- **Swagger / OpenAPI**

## Prompt pipeline

The prompt-polishing layer is intentionally separated behind an interface so the implementation can be changed without rewriting the request flow. The current code includes OpenAI and Groq-oriented implementations.

The pipeline also applies guardrails intended to keep submitted concepts appropriate for a family-friendly game and converts recognizable brands or characters into more generic visual ideas.

## API examples

```text
POST /api/FlexImage/generate
GET  /api/FlexCrop
POST /api/FlexCrop/{id}/credit
POST /api/FlexCrop/{id}/like
GET  /api/config
```

## Configuration

The application expects external service credentials and database/storage settings to be supplied through configuration. Do not commit real API keys, passwords, or connection strings to source control.

Example configuration values include:

```text
OpenAI:ApiKey
Groq:ApiKey
Azure:BlobConnectionString
Azure:ContainerName
ConnectionStrings:FlexDb
```

## Why I built it

FlexOMatic started as a practical way to let players suggest unusual crop ideas without manually creating every concept. It became an experiment in connecting LLM-driven prompt transformation, image generation, backend APIs, persistence, and a simple user-facing workflow into one small system.

It is intentionally a hands-on project: less about model research and more about using AI inside a real application pipeline.
