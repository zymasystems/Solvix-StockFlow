# Solvix StockFlow

Solvix StockFlow is the Zyma Systems inventory and logistics platform. This repository keeps its web and mobile applications in separate directories.

## Projects

- `web/` — existing PHP and MySQL website.
- `mobile/` — .NET MAUI Android application.

## Mobile app

Requirements: .NET 10 SDK, MAUI Android workload, and Android SDK.

```bash
dotnet build mobile/Solvix.Sims.csproj -t:Run -f net10.0-android
```

Tailwind tooling is in `mobile/tools/tailwind`.

## Website

See `web/README.md` for setup and deployment details.
