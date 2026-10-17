# Проєкт F1-Hub

## Вимоги
- .NET SDK 9 або новіший
- Node.js 20+ і npm

## Структура
- `src/F1Hub.Api` — Web API
- `src/F1Hub.Domain` — доменні моделі
- `tests/F1Hub.Tests` — тести
- `client` — React-застосунок (Vite)

## Запуск API
```bash
dotnet run --project src/F1Hub.Api
```

## Запуск клієнта
```bash
cd client
npm install
npm run dev
```

Клієнт відкриється на http://localhost:5173

## Тести
```bash
dotnet test
```

## Гілки
- `main` — стабільна версія
- `develop` — розробка
- `feature/*` — нові функції, створюються від `develop`