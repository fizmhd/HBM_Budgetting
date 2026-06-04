# BudgetTracker API Integration Tests

This project contains integration tests for the BudgetTracker API, with a focus on security features.

## Test Structure

### Security Tests

Located in `Security/` directory:

- **CsrfProtectionTests.cs** - Tests for CSRF token generation and validation
- **RateLimitingTests.cs** - Tests for rate limiting enforcement
- **PasswordValidationTests.cs** - Tests for password strength validation
- **JwtValidationTests.cs** - Tests for JWT token validation
- **AccountLockoutTests.cs** - Tests for account lockout functionality

### Test Infrastructure

- **CustomWebApplicationFactory.cs** - Custom factory that boots the API in the `Testing`
  environment against a real **PostgreSQL 16 Testcontainer**, migrates the schema on startup, and
  resets state between tests with **Respawn** (`DbAdapter.Postgres`). Requires a running container
  runtime (Docker Desktop); endpoint configured in `testcontainers.properties`.

## Known failing tests (recorded Phase 0, 2026-06-03)

The Phase 0 SQL Server → PostgreSQL switch left the test **infrastructure** correct (container +
Respawn, no `MsSql`/`UseSqlServer`). With that in place **48/68 integration tests pass**. The
remaining **20 failures pre-date the DB switch** and are *not* Postgres-related — they are
test-harness gaps deferred to a later test-hardening pass:

1. **Auth happy-path tests** (login/register/profile/settings round-trips) — `CustomWebApplicationFactory`
   registers `Substitute.For<IAuthProvider>()` but never configures `.Returns(...)`, so login issues
   no token and the authenticated calls return 401. Fix = stub a successful auth result in the factory.
2. **AccountLockout & RateLimiting tests** — assert lockout/throttling behaviour, but
   `appsettings.Testing.json` deliberately sets `Lockout.Enabled` and `RateLimiting.Enabled` to
   `false`. Fix = exercise these via a dedicated config that enables the feature under test.

None of the 20 emit `Npgsql`/SQL errors; they fail on HTTP status-code assertions only.

## Running Tests

### Run All Tests

```bash
dotnet test
```

### Run Specific Test Class

```bash
dotnet test --filter "FullyQualifiedName~CsrfProtectionTests"
dotnet test --filter "FullyQualifiedName~RateLimitingTests"
dotnet test --filter "FullyQualifiedName~PasswordValidationTests"
dotnet test --filter "FullyQualifiedName~JwtValidationTests"
dotnet test --filter "FullyQualifiedName~AccountLockoutTests"
```

### Run Specific Test

```bash
dotnet test --filter "FullyQualifiedName~PasswordValidationTests.Register_WithWeakPassword_ShouldReturnValidationError"
```

## Test Coverage

### CSRF Protection
- ✅ POST requests without CSRF token return 403
- ✅ Login endpoint excluded from CSRF validation
- ✅ Register endpoint excluded from CSRF validation
- ✅ Successful login sets CSRF cookie
- ✅ GET requests don't require CSRF token

### Rate Limiting
- ✅ Login endpoint enforces 5 requests per 15 minutes
- ✅ Register endpoint enforces 3 requests per hour
- ✅ Rate limit exceeded returns 429 with Retry-After header
- ✅ Rate limit response contains error message

### Password Validation
- ✅ Weak passwords (too short, missing uppercase, lowercase, digit, special char) are rejected
- ✅ Strong passwords are accepted
- ✅ Mismatched passwords are rejected
- ✅ Too long passwords are rejected
- ✅ Empty passwords are rejected
- ✅ Invalid email format is rejected

### JWT Validation
- ✅ Protected endpoints without token return 401
- ✅ Invalid tokens return 401
- ✅ Malformed tokens return 401
- ✅ Expired tokens return 401
- ✅ Tokens without Bearer prefix don't authenticate
- ✅ Anonymous endpoints work with invalid tokens

### Account Lockout
- ✅ Multiple failed login attempts lock the account
- ✅ Locked account shows remaining lockout time
- ✅ Successful login resets failed attempts

## Notes

### Test Dependencies

- **xUnit** - Test framework
- **FluentAssertions** - Assertion library
- **Microsoft.AspNetCore.Mvc.Testing** - Integration testing framework
- **In-Memory Database** - For isolated test data

### Test Limitations

1. **Supabase Integration**: Tests use in-memory database and may not fully test Supabase authentication flow
2. **Rate Limiting**: Tests assume clean state; running tests multiple times quickly may hit actual rate limits
3. **CSRF**: Some tests check for 403 or 404 depending on whether endpoints exist
4. **JWT**: Tests use sample/invalid tokens; real token generation would require Supabase setup

### Recommendations

1. **Mock Supabase**: Consider mocking Supabase auth provider for more reliable tests
2. **Test Isolation**: Ensure each test uses unique data to avoid conflicts
3. **Configuration**: Consider using test-specific configuration for rate limits and lockout settings
4. **Real Tokens**: For comprehensive JWT tests, generate real tokens with test user accounts

## Future Enhancements

- [ ] Token rotation tests
- [ ] Token reuse detection tests
- [ ] Session limit enforcement tests
- [ ] User context middleware tests
- [ ] End-to-end authentication flow tests
- [ ] Performance tests for rate limiting
- [ ] Concurrent request tests
