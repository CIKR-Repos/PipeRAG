# Contributing to PipeRAG

Thank you for your interest in contributing to PipeRAG! 🎉

## 🚀 Getting Started

1. Fork the repository
2. Clone your fork: `git clone https://github.com/YOUR_USERNAME/PipeRAG.git`
3. Create a branch: `git checkout -b feat/your-feature`
4. Make your changes
5. Run tests: `dotnet test`
6. Commit: `git commit -m 'feat: description of your change'`
7. Push: `git push origin feat/your-feature`
8. Open a Pull Request

## 📋 Development Setup

### Prerequisites
- .NET 10 SDK
- Node.js 22+
- Docker (for PostgreSQL + pgvector)
- Angular CLI (`npm install -g @angular/cli`)

### Running Locally

```bash
# Start database
docker compose up -d db

# Run API
cd src/PipeRAG.Api
dotnet ef database update
dotnet run

# Run frontend (separate terminal)
cd client
npm install
ng serve
```

## 🏗️ Project Structure

```
PipeRAG/
├── src/
│   ├── PipeRAG.Api/            # ASP.NET Web API (controllers, middleware)
│   ├── PipeRAG.Core/           # Domain entities, interfaces, DTOs
│   └── PipeRAG.Infrastructure/ # EF Core, services, external integrations
├── client/                     # Angular 21 SPA
├── tests/
│   └── PipeRAG.Tests/          # xUnit tests
├── docs/                       # Architecture & feature docs
└── docker-compose.yml
```

## 📝 Commit Convention

We use [Conventional Commits](https://www.conventionalcommits.org/):

- `feat:` — New feature
- `fix:` — Bug fix
- `docs:` — Documentation changes
- `refactor:` — Code refactoring
- `test:` — Adding or updating tests
- `chore:` — Build/tooling changes

## 🧪 Testing

- Write tests for new features (aim for >80% coverage)
- Run all tests before submitting: `dotnet test`
- Angular tests: `cd client && ng test`

## 🎨 Code Style

- **Backend**: Follow C# conventions, use Clean Architecture patterns
- **Frontend**: Angular 21 Signals, standalone components, Tailwind CSS (no plain SCSS)
- **General**: Keep PRs focused and under 150K diff characters

## 💬 Questions?

Open a [Discussion](https://github.com/CIKR-Repos/PipeRAG/discussions) or file an [Issue](https://github.com/CIKR-Repos/PipeRAG/issues).

Thank you for helping make RAG accessible to everyone! ❤️
