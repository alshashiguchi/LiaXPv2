# LiaXP - AI-Powered Sales Assistant API

[![.NET 8](https://img.shields.io/badge/.NET-8.0-purple)](https://dotnet.microsoft.com/)
[![Azure SQL](https://img.shields.io/badge/Azure-SQL-blue)](https://azure.microsoft.com/en-us/services/sql-database/)
[![WhatsApp](https://img.shields.io/badge/WhatsApp-Integration-green)](https://www.whatsapp.com/)

Enterprise-grade sales intelligence platform with multi-company support, AI-driven insights, and WhatsApp integration.

## 🎯 Features

- **Multi-Company Architecture**: Complete isolation with `companyCode` scope
- **Excel Data Import**: Automated import of sales, goals, and team data
- **AI Insights**: Real-time sales analytics, gap analysis, and projections
- **WhatsApp Integration**: Support for both Twilio (MVP) and Meta Cloud API (production)
- **Human-in-the-Loop (HITL)**: Manual review and approval workflow for automated messages
- **Cron Jobs**: Scheduled automated messages (morning, midday, evening)
- **Intent Recognition**: Smart routing of chat messages (goal gap, tips, ranking, etc.)
- **Enterprise Security**: JWT authentication, RBAC, rate limiting, CORS, CSP headers
- **Clean Architecture**: DDD, SOLID principles, testable code

## 🏗️ Architecture

```
┌─────────────┐
│   Client    │
└─────┬───────┘
      │
      ▼
┌─────────────────────────────────┐
│        LiaXP.Api                │
│  (Controllers, Middleware)      │
└─────────────┬───────────────────┘
              │
              ▼
┌─────────────────────────────────┐
│    LiaXP.Application            │
│     (Use Cases, DTOs)           │
└─────────────┬───────────────────┘
              │
      ┌───────┴────────┐
      │                │
      ▼                ▼
┌─────────────┐  ┌────────────────┐
│   Domain    │  │ Infrastructure │
│  (Entities, │  │  (Repositories,│
│  Interfaces)│  │   Services)    │
└─────────────┘  └────────┬───────┘
                          │
                          ▼
                  ┌───────────────┐
                  │  Azure SQL    │
                  │   WhatsApp    │
                  └───────────────┘
```

## 📋 Prerequisites

- .NET 8 SDK
- Azure SQL Server (or SQL Server 2019+)
- Azure account (for deployment)
- Twilio account (for MVP) or Meta Business account (for production)

## 🚀 Quick Start

### 1. Clone the Repository

```bash
git clone https://github.com/alshashiguchi/liaxp.git
cd liaxp
```

### 2. Setup Database

```bash
# Run the initialization script on your Azure SQL Server
sqlcmd -S your-server.database.windows.net -U your-user -P your-password -d liaxp -i scripts/init-db.sql
```

### 3. Configure Environment Variables

```bash
# Copy example environment file
cp .env.example .env

# Edit .env with your credentials
nano .env
```

**Required variables:**
- `AZURE_SQL_CONNECTIONSTRING`: Your Azure SQL connection string
- `JWT_SIGNING_KEY`: Strong secret key (min 32 characters)
- `TWILIO_ACCOUNT_SID`: Twilio Account SID (for MVP)
- `TWILIO_AUTH_TOKEN`: Twilio Auth Token (for MVP)

### 4. Run with Docker

```bash
# Build and start the API
docker-compose up -d

# View logs
docker-compose logs -f

# Stop
docker-compose down
```

The API will be available at:
- **API**: http://localhost:8080
- **Swagger**: http://localhost:8080/swagger
- **Health**: http://localhost:8080/healthz

### 5. Run Locally (without Docker)

```bash
# Restore dependencies
dotnet restore

# Run the API
cd src/LiaXP.Api
dotnet run

# Or with watch (auto-reload)
dotnet watch run
```

## 📚 API Documentation

### Authentication

All endpoints (except public ones) require JWT authentication and company scope.

**Headers:**
```
Authorization: Bearer <jwt-token>
X-Company-Code: <your-company-code>
```

### Public Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/healthz` | Health check |
| POST | `/auth/token` | Login (MVP) |
| GET/POST | `/webhook/whatsapp` | WhatsApp webhook |

### Protected Endpoints

| Method | Endpoint | Auth | Description |
|--------|----------|------|-------------|
| POST | `/data/import/xlsx` | Admin/Manager | Import Excel data |
| GET | `/data/status` | Admin/Manager | Get import/training status |
| POST | `/train` | Admin/Manager | Retrain insights |
| POST | `/cron/run-now` | Admin/Manager | Trigger manual message generation |
| GET | `/insights` | Any | Get insights |
| POST | `/chat` | Any | Chat with AI |
| GET | `/reviews/pending` | Admin/Manager | Get pending reviews |
| POST | `/reviews/{id}/approve` | Admin/Manager | Approve and send message |
| POST | `/reviews/{id}/edit-and-approve` | Admin/Manager | Edit and send message |

## 📝 Usage Examples

### Import Excel Data

```bash
curl -X POST "http://localhost:8080/data/import/xlsx?retrain=true" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "X-Company-Code: ACME" \
  -F "file=@./data/sales_data.xlsx"
```

### Retrain Model

```bash
curl -X POST "http://localhost:8080/train" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "X-Company-Code: ACME"
```

### Trigger Cron Job Manually

```bash
curl -X POST "http://localhost:8080/cron/run-now?moment=morning&send=false" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "X-Company-Code: ACME"
```

### Get Insights

```bash
curl "http://localhost:8080/insights?sellerCode=S001" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "X-Company-Code: ACME"
```

### Chat

```bash
curl -X POST "http://localhost:8080/chat" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "X-Company-Code: ACME" \
  -H "Content-Type: application/json" \
  -d '{"message":"quanto falta pra meta?"}'
```

### Review Queue

```bash
# Get pending reviews
curl "http://localhost:8080/reviews/pending" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "X-Company-Code: ACME"

# Approve and send
curl -X POST "http://localhost:8080/reviews/REVIEW_ID/approve" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "X-Company-Code: ACME"
```

## 🔒 Security

### JWT Configuration

**Local Development (HS256):**
```bash
JWT_ISSUER=self
JWT_SIGNING_KEY=your-32-char-secret-key
JWT_AUDIENCE=liaxp-api
```

**Production (Azure AD / RS256):**
```bash
JWT_ISSUER=https://sts.windows.net/{tenant-id}/
JWT_AUTHORITY=https://login.microsoftonline.com/{tenant-id}/v2.0
JWT_AUDIENCE={app-client-id}
```

### Company Scope

Every request must include the `X-Company-Code` header, which is validated against the JWT token claims to ensure proper company isolation.

### Rate Limiting

- **Authenticated endpoints**: 60 requests/minute
- **Public endpoints**: 30 requests/minute

## 📲 WhatsApp Integration

### Option 1: Twilio (MVP/Quick Start)

```bash
WHATS_PROVIDER=twilio
TWILIO_ACCOUNT_SID=AC...
TWILIO_AUTH_TOKEN=...
TWILIO_FROM=whatsapp:+14155238886
```

**Webhook setup:**
1. Go to Twilio Console > WhatsApp Sandbox
2. Set webhook URL: `https://your-domain.com/webhook/whatsapp`
3. Method: POST

### Option 2: Meta Cloud API (Production)

```bash
WHATS_PROVIDER=meta
META_WA_TOKEN=...
META_WA_PHONE_ID=...
META_WA_VERIFY_TOKEN=...
META_WA_APP_SECRET=...
```

**Webhook setup:**
1. Go to Meta Developer Console
2. Configure webhook: `https://your-domain.com/webhook/whatsapp`
3. Verify token: Use your `META_WA_VERIFY_TOKEN`

## 📊 Excel File Format

The Excel file should contain these sheets:

| Sheet Name | Required Columns |
|------------|------------------|
| **Sales** | date, store, seller_code, seller_name, total_value, items_qty, avg_ticket, category |
| **Goals** | month, store, seller_code, target_value, target_ticket, target_conversion |
| **Team** | seller_code, seller_name, store, phone_e164, status |

**Example:** See `docs/example_data.xlsx`

## 🧪 Testing

```bash
# Run all tests
dotnet test

# Run with coverage
dotnet test /p:CollectCoverage=true
```

## 🚢 Deployment

### Azure App Service

```bash
# Login to Azure
az login

# Create resource group
az group create --name liaxp-rg --location eastus

# Create App Service plan
az appservice plan create --name liaxp-plan --resource-group liaxp-rg --sku B1 --is-linux

# Create Web App
az webapp create --resource-group liaxp-rg --plan liaxp-plan --name liaxp-api --runtime "DOTNETCORE:8.0"

# Deploy
az webapp deployment source config-zip --resource-group liaxp-rg --name liaxp-api --src ./publish.zip

# Configure environment variables
az webapp config appsettings set --resource-group liaxp-rg --name liaxp-api --settings @env-vars.json
```

### Docker

```bash
# Build image
docker build -t liaxp-api:latest .

# Run container
docker run -d -p 8080:80 --env-file .env liaxp-api:latest
```

## 🔧 Development

### Project Structure

```
LiaXP/
├── src/
│   ├── LiaXP.Domain/          # Core domain entities, interfaces
│   ├── LiaXP.Application/     # Use cases, business logic
│   ├── LiaXP.Infrastructure/  # Data access, external services
│   └── LiaXP.Api/            # REST API, controllers
├── tests/
│   └── LiaXP.Tests/          # Unit and integration tests
├── scripts/
│   └── init-db.sql           # Database initialization
├── docs/                      # Documentation
├── Dockerfile
├── docker-compose.yml
└── README.md
```

### Adding a New Feature

1. Define interface in `Domain/Interfaces/`
2. Implement in `Infrastructure/Services/`
3. Create use case in `Application/UseCases/`
4. Add controller in `Api/Controllers/`
5. Register in `Program.cs`
6. Write tests in `Tests/`

## 📖 Documentation

- [Architecture Guide](docs/ARCHITECTURE.md)
- [API Reference](docs/API.md)
- [Security Guide](docs/SECURITY.md)
- [Deployment Guide](docs/DEPLOYMENT.md)

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/amazing-feature`)
3. Commit your changes (`git commit -m 'Add amazing feature'`)
4. Push to the branch (`git push origin feature/amazing-feature`)
5. Open a Pull Request

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 💬 Support

- **Issues**: [GitHub Issues](https://github.com/alshashiguchi/liaxp/issues)
- **Discussions**: [GitHub Discussions](https://github.com/alshashiguchi/liaxp/discussions)

## 🙏 Acknowledgments

- Built with [.NET 8](https://dotnet.microsoft.com/)
- Uses [Dapper](https://github.com/DapperLib/Dapper) for data access
- Powered by [Azure SQL](https://azure.microsoft.com/services/sql-database/)
- Integrated with [WhatsApp Business API](https://developers.facebook.com/docs/whatsapp)

---

**Developed with ❤️ following Clean Architecture principles**
