# Solvix StockFlow

Solvix StockFlow is the Zyma Systems inventory and logistics platform. This repository contains the browser website and the Android mobile app in separate directories.

## Projects

- `web/` — PHP and MySQL website.
- `mobile/` — .NET MAUI Android app. Its screens, models, services, and static assets are under this project.

## Mobile app

Requirements: .NET 10 SDK, MAUI Android workload, and Android SDK.

```bash
dotnet build mobile/Solvix.Sims.csproj -t:Run -f net10.0-android
```

Tailwind CSS tooling lives in `mobile/tools/tailwind`.

## Website

See `web/README.md` for the PHP site setup and deployment notes.
