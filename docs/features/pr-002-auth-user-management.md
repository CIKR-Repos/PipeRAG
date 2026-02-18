# PR #2: Authentication + User Management

## Overview
Custom JWT-based authentication system with user registration, login, token refresh, profile management, and per-tier rate limiting.

## Backend

### Auth Endpoints
| Method | Path | Description | Auth |
|--------|------|-------------|------|
| POST | `/api/auth/register` | Register new user | No |
| POST | `/api/auth/login` | Login with email/password | No |
| POST | `/api/auth/refresh` | Refresh access token | No |
| GET | `/api/users/me` | Get current user profile | Yes |
| PUT | `/api/users/me` | Update profile | Yes |

### JWT Configuration (`appsettings.json`)
```json
{
  "Jwt": {
    "Secret": "your-secret-key-min-32-chars",
    "Issuer": "PipeRAG",
    "Audience": "PipeRAG",
    "AccessTokenExpiryMinutes": 15,
    "RefreshTokenExpiryDays": 7
  }
}
```

### Custom Claims
- `UserId` — User GUID
- `Email` — User email
- `Tier` — Free / Pro / Enterprise

### Rate Limiting (per tier)
| Tier | Requests/Hour |
|------|---------------|
| Free | 100 |
| Pro | 1,000 |
| Enterprise | 10,000 |

Returns `429 Too Many Requests` with `Retry-After` header when exceeded.

### Entities
- `RefreshToken` — stores refresh tokens with expiry and revocation tracking

### Validation (FluentValidation)
- `RegisterRequest`: email format, password min 8 chars, display name required
- `LoginRequest`: email and password required
- `UpdateProfileRequest`: optional fields validated when present
- `RefreshTokenRequest`: token required

## Frontend (Angular 21)

### Components
- `LoginComponent` — standalone, uses signals for loading/error state
- `RegisterComponent` — standalone, same pattern

### Services
- `AuthService` — signal-based state (`user`, `isAuthenticated`, `userTier`), localStorage persistence

### Infrastructure
- `authInterceptor` — `HttpInterceptorFn` that attaches Bearer token
- `authGuard` — `CanActivateFn` that redirects to `/login`

## Tests (22 total)
- **AuthServiceTests** (8): register, login, refresh, duplicate email, wrong password, token reuse
- **RateLimitingMiddlewareTests** (3): under limit, exceeded, skip non-API
- **ValidationTests** (7): valid/invalid inputs for all request DTOs

## Packages Added
- `Microsoft.AspNetCore.Authentication.JwtBearer`
- `BCrypt.Net-Next`
- `FluentValidation.AspNetCore`
- `System.IdentityModel.Tokens.Jwt`
- `Microsoft.EntityFrameworkCore.InMemory` (test support)
