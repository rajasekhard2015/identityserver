# Identity Server API

A comprehensive ASP.NET Core 8.0 Identity Server with OAuth client management and permission-based authorization.

## Features

- **ASP.NET Core Identity**: Complete user and role management using Entity Framework Core
- **Permission-based Authorization**: Custom attribute-based authorization system with granular permissions
- **OAuth Client Management**: Full CRUD operations for OAuth clients with secure ClientId/ClientSecret generation
- **JWT Authentication**: Stateless authentication using JSON Web Tokens
- **Swagger Documentation**: Complete API documentation with JWT authentication support
- **SQLite Database**: Portable database for easy development and deployment
- **Security Best Practices**: Secrets are never exposed in list endpoints, password hashing, secure token generation

## Quick Start

1. **Clone and Run**:
   ```bash
   git clone <repository-url>
   cd identityserver
   dotnet run
   ```

2. **Access the API**:
   - **Swagger UI**: http://localhost:5221/
   - **API Base URL**: http://localhost:5221/api/

3. **Default Admin Account**:
   - **Email**: admin@identityserver.local
   - **Password**: Admin123!

## API Endpoints

### Authentication
- `POST /api/auth/register` - Register new user
- `POST /api/auth/login` - Login user
- `GET /api/auth/me` - Get current user info
- `POST /api/auth/logout` - Logout user

### User Management (Requires `users.*` permissions)
- `GET /api/users` - List all users (paginated)
- `GET /api/users/{id}` - Get user by ID
- `POST /api/users` - Create new user
- `PUT /api/users/{id}` - Update user
- `DELETE /api/users/{id}` - Delete user

### Role Management (Requires `roles.*` permissions)
- `GET /api/roles` - List all roles with permissions
- `GET /api/roles/{id}` - Get role by ID
- `POST /api/roles` - Create new role
- `PUT /api/roles/{id}` - Update role
- `DELETE /api/roles/{id}` - Delete role

### Permissions (Requires `permissions.read` permission)
- `GET /api/permissions` - List all permissions
- `GET /api/permissions/by-category` - Get permissions grouped by category
- `GET /api/permissions/{id}` - Get permission by ID

### OAuth Client Management (Requires `oauth.*` permissions)
- `GET /api/oauthclients` - List OAuth clients (secrets not included)
- `GET /api/oauthclients/{id}` - Get OAuth client by ID
- `POST /api/oauthclients` - Create new OAuth client
- `PUT /api/oauthclients/{id}` - Update OAuth client
- `POST /api/oauthclients/{id}/regenerate-secret` - Regenerate client secret
- `PATCH /api/oauthclients/{id}/status` - Activate/deactivate client
- `DELETE /api/oauthclients/{id}` - Delete OAuth client

## Permission System

The API uses a granular permission system with the following permissions:

### User Permissions
- `users.read` - View users
- `users.create` - Create users
- `users.update` - Update users
- `users.delete` - Delete users

### Role Permissions
- `roles.read` - View roles
- `roles.create` - Create roles
- `roles.update` - Update roles
- `roles.delete` - Delete roles

### Permission Permissions
- `permissions.read` - View permissions

### OAuth Permissions
- `oauth.read` - View OAuth clients
- `oauth.create` - Create OAuth clients
- `oauth.update` - Update OAuth clients
- `oauth.delete` - Delete OAuth clients

## Usage Examples

### 1. Register a New User
```bash
curl -X POST "http://localhost:5221/api/auth/register" \
  -H "Content-Type: application/json" \
  -d '{
    "email": "user@example.com",
    "password": "Password123!",
    "firstName": "John",
    "lastName": "Doe"
  }'
```

### 2. Login and Get Token
```bash
curl -X POST "http://localhost:5221/api/auth/login" \
  -H "Content-Type: application/json" \
  -d '{
    "email": "admin@identityserver.local",
    "password": "Admin123!"
  }'
```

### 3. Create OAuth Client
```bash
curl -X POST "http://localhost:5221/api/oauthclients" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "My Application",
    "description": "My OAuth application",
    "redirectUri": "https://myapp.com/callback",
    "allowedScopes": "openid profile email"
  }'
```

### 4. Get Users (Admin Only)
```bash
curl -X GET "http://localhost:5221/api/users" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN"
```

## Database

The application uses SQLite with the following key entities:
- **ApplicationUser** - Extended IdentityUser with additional fields
- **ApplicationRole** - Extended IdentityRole with permissions
- **Permission** - Granular permissions for authorization
- **RolePermission** - Many-to-many relationship between roles and permissions
- **OAuthClient** - OAuth client applications with secure credentials

## Security Features

- **JWT Authentication** - Stateless authentication with configurable expiration
- **Permission-based Authorization** - Granular access control using custom attributes
- **Secure Secret Generation** - Cryptographically secure client secrets and tokens
- **Password Hashing** - ASP.NET Core Identity password hashing
- **Secret Protection** - Client secrets are never returned in list operations
- **Input Validation** - Comprehensive model validation on all endpoints

## Configuration

Key configuration in `appsettings.json`:
- **ConnectionStrings:DefaultConnection** - SQLite database path
- **JWT:Secret** - Secret key for JWT signing
- **JWT:ValidIssuer** - JWT issuer
- **JWT:ValidAudience** - JWT audience

## Development

- **Framework**: .NET 8.0
- **Database**: SQLite with Entity Framework Core
- **Authentication**: ASP.NET Core Identity with JWT
- **Documentation**: Swagger/OpenAPI
- **Logging**: Built-in ASP.NET Core logging

The application automatically creates the database and seeds default data on startup.