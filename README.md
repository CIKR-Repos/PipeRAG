<p align="center">
  <img src="https://img.shields.io/badge/PipeRAG-No--Code%20RAG-6366f1?style=for-the-badge&logo=data:image/svg+xml;base64,PHN2ZyB4bWxucz0iaHR0cDovL3d3dy53My5vcmcvMjAwMC9zdmciIHdpZHRoPSIyNCIgaGVpZ2h0PSIyNCIgdmlld0JveD0iMCAwIDI0IDI0IiBmaWxsPSJ3aGl0ZSI+PHBhdGggZD0iTTMgM2gxOHYxOEgzeiIvPjwvc3ZnPg==" alt="PipeRAG" />
</p>

<h1 align="center">🔗 PipeRAG</h1>

<p align="center">
  <strong>No-code RAG pipeline builder for non-technical users.</strong><br/>
  Upload files → pick AI → get chatbot. Zero code. Grandma-simple.
</p>

<p align="center">
  <a href="https://github.com/CIKR-Repos/PipeRAG/stargazers"><img src="https://img.shields.io/github/stars/CIKR-Repos/PipeRAG?style=social" alt="Stars" /></a>
  <a href="https://github.com/CIKR-Repos/PipeRAG/fork"><img src="https://img.shields.io/github/forks/CIKR-Repos/PipeRAG?style=social" alt="Forks" /></a>
  <a href="https://github.com/CIKR-Repos/PipeRAG/issues"><img src="https://img.shields.io/github/issues/CIKR-Repos/PipeRAG" alt="Issues" /></a>
  <a href="https://github.com/CIKR-Repos/PipeRAG/blob/main/LICENSE"><img src="https://img.shields.io/github/license/CIKR-Repos/PipeRAG" alt="License" /></a>
  <a href="https://github.com/CIKR-Repos/PipeRAG/actions"><img src="https://img.shields.io/github/actions/workflow/status/CIKR-Repos/PipeRAG/ci.yml?label=CI" alt="CI" /></a>
</p>

<p align="center">
  <a href="#-features">Features</a> •
  <a href="#-quick-start">Quick Start</a> •
  <a href="#-architecture">Architecture</a> •
  <a href="#-screenshots">Screenshots</a> •
  <a href="#-roadmap">Roadmap</a> •
  <a href="#-contributing">Contributing</a>
</p>

---

## 🤔 The Problem

Building a RAG (Retrieval-Augmented Generation) chatbot today requires:
- Writing hundreds of lines of code
- Understanding embeddings, vector databases, chunking strategies
- Setting up infrastructure, APIs, and deployment pipelines
- Weeks of development time

**Most people who need RAG chatbots aren't developers.**

## ✨ The Solution

PipeRAG lets anyone build a production-ready RAG chatbot in **30 seconds**:

1. **📄 Upload** your documents (PDF, DOCX, TXT, MD, CSV)
2. **⚡ Auto-pipeline** handles chunking, embedding, and storage
3. **💬 Chat** with your documents instantly
4. **🔗 Embed** the chatbot anywhere with one `<script>` tag

No code. No config. No PhD in AI required.

## 🚀 Features

| Feature | Description |
|---------|-------------|
| 📄 **Smart Document Processing** | Upload PDF, DOCX, TXT, MD, CSV — auto-parsed and chunked |
| 🔗 **Visual Pipeline Builder** | Drag-and-drop pipeline configuration (Source → Chunk → Embed → Retrieve → Generate) |
| ⚡ **Auto-Pipeline** | Zero-config mode: upload → chatbot in 30 seconds |
| 🧬 **Multiple Embedding Models** | text-embedding-3-small, text-embedding-3-large, ada-002 |
| 💬 **Streaming Chat** | Real-time SSE streaming responses with conversation memory |
| 🎨 **Embeddable Widget** | One `<script>` tag to add a chatbot to any website |
| 🎯 **Chunk Preview** | See exactly how your documents are split before processing |
| 💳 **Billing & Tiers** | Free/Pro/Enterprise with Stripe integration |
| 📊 **Dashboard & Analytics** | Project management, usage tracking, query analytics |
| 🐳 **One-Click Deploy** | Docker + GitHub Actions + fly.io ready |

## 🏗️ Architecture

```
┌─────────────────────────────────────────────────────┐
│                    Angular 21 SPA                     │
│         (Signals · Standalone · Tailwind CSS)         │
├─────────────────────────────────────────────────────┤
│                   .NET 10 Web API                     │
│              (Clean Architecture · CQRS)              │
├──────────┬──────────┬──────────┬────────────────────┤
│ Semantic │ pgvector │  Redis   │    PostgreSQL       │
│  Kernel  │ (vectors)│ (cache)  │    (data store)     │
└──────────┴──────────┴──────────┴────────────────────┘
```

- **Frontend**: Angular 21 (Signals, zoneless, standalone components) + Tailwind CSS
- **Backend**: .NET 10 + Clean Architecture (Api / Core / Infrastructure)
- **AI/ML**: Microsoft Semantic Kernel for embeddings + LLM orchestration
- **Vector DB**: pgvector (runs inside PostgreSQL — no extra service!)
- **Cache**: Redis for rate limiting + session cache
- **Database**: PostgreSQL for all relational data

## 📸 Screenshots

> _Screenshots coming soon! Run locally to see PipeRAG in action._

## ⚡ Quick Start

### Prerequisites
- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Node.js 22+](https://nodejs.org/)
- [Docker](https://www.docker.com/) (for PostgreSQL + pgvector)

### 1. Clone & Setup

```bash
git clone https://github.com/CIKR-Repos/PipeRAG.git
cd PipeRAG
```

### 2. Start Database

```bash
docker compose up -d db
```

### 3. Run API

```bash
cd src/PipeRAG.Api
dotnet ef database update
dotnet run
```

### 4. Run Frontend

```bash
cd client
npm install
ng serve
```

### 5. Open Browser

Navigate to `http://localhost:4200` — register an account and start building!

### 🐳 Docker (Full Stack)

```bash
docker compose --profile production up -d
```

## 🗺️ Roadmap

- [x] 📄 Document processing (PDF, DOCX, TXT, MD, CSV)
- [x] 🔗 Visual pipeline builder with drag-and-drop
- [x] ⚡ Auto-pipeline (zero-config RAG)
- [x] 💬 Streaming chat with conversation memory
- [x] 🎨 Embeddable chat widget
- [x] 💳 Stripe billing integration
- [x] 🐳 Docker + CI/CD deployment
- [ ] 🌐 Multi-language support
- [ ] 🔌 API connectors (Notion, Confluence, Google Drive)
- [ ] 📱 Mobile-responsive widget
- [ ] 🤖 More LLM providers (Anthropic, Ollama, Mistral)
- [ ] 📊 Advanced analytics dashboard
- [ ] 🔐 SSO / SAML authentication
- [ ] 🏢 Multi-tenant / white-label support

## 🤝 Contributing

We love contributions! Whether it's bug reports, feature requests, or code — all are welcome.

1. **Fork** the repository
2. **Create** a feature branch (`git checkout -b feat/amazing-feature`)
3. **Commit** your changes (`git commit -m 'feat: add amazing feature'`)
4. **Push** to the branch (`git push origin feat/amazing-feature`)
5. **Open** a Pull Request

See [CONTRIBUTING.md](CONTRIBUTING.md) for detailed guidelines.

## 📄 License

This project is licensed under the MIT License — see the [LICENSE](LICENSE) file for details.

## 💬 Support

- 🐛 [Report a Bug](https://github.com/CIKR-Repos/PipeRAG/issues/new?template=bug_report.md)
- 💡 [Request a Feature](https://github.com/CIKR-Repos/PipeRAG/issues/new?template=feature_request.md)
- 💬 [Start a Discussion](https://github.com/CIKR-Repos/PipeRAG/discussions)
- ⭐ **Star this repo** if you find it useful!

---

<p align="center">
  Built with ❤️ by <a href="https://github.com/CIKR-Repos">CIKR-Repos</a>
</p>

<p align="center">
  <a href="https://github.com/CIKR-Repos/PipeRAG/stargazers">
    <img src="https://img.shields.io/github/stars/CIKR-Repos/PipeRAG?style=for-the-badge&color=yellow" alt="Star PipeRAG" />
  </a>
</p>
