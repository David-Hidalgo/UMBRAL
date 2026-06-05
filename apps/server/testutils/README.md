Test utilities for integration/unit tests across services.

Usage

- Reference this project from your test project with a `ProjectReference`.
- Call `services.AddTestAuthentication()` in your test host configuration to enable a test auth scheme that authenticates requests with a default `admin` role.

Example (in `WebApplicationFactory` setup):

```csharp
builder.ConfigureServices(services =>
{
    services.AddTestAuthentication();
});
```

The `TestAuthHandler` returns a principal with `ClaimTypes.NameIdentifier` = `test-user` and a role `admin`.
