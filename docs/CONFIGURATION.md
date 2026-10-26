# Configuration

## Database connection string

The committed `appsettings.json` does **not** contain the database password.
Provide the full connection string through one of these (highest priority first):

1. Environment variable `DTM_CONNECTION_STRING`
2. Environment variable `ConnectionStrings__DefaultConnection`
3. `appsettings.local.json` next to the executable (git-ignored):

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=DailyTaskManager;Username=postgres;Password=<your password>;"
  }
}
```

If none is set the app stops at startup with a message saying so.
The Settings page masks the password when it shows the connection string.

## Google sign-in

Set `OAuth:Google:ClientId` and `OAuth:Google:ClientSecret` in `appsettings.local.json`
(or provide a `client_secret.json` next to the executable). Without them the
"Continue with Google" button reports that Google login is not configured.

## Update signature verification

Set `UpdateService:Ed25519PublicKey` to verify appcast signatures. Without it,
updates are accepted unsigned and a warning is logged at startup.
