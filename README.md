# Castwork — Frontend

Frontend Angular do Castwork, sistema para joalheiros artesanais.

## Requisitos
- Node 22.22+ e npm 11 (o npm 10 tem um bug com as peer deps do Vitest: use `npx npm@11 install`)

## Scripts
| Comando | O que faz |
|---|---|
| `npm start` | servidor de desenvolvimento (environment.development.ts) |
| `npm run build` | build de produção |
| `npm run lint` | ESLint com `maxWarnings: 0` |
| `npm test` | Vitest em modo watch |
| `npm run test:ci` | Vitest uma vez |

## Camadas
Veja o README de cada pasta em `src/app/` (`core`, `data`, `shared`, `layout`, `modules`).
