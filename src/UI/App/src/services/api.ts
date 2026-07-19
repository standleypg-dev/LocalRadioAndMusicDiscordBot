// Production builds use a relative path because the Worker serves the SPA and
// the API from the same origin (works for local docker and deployment alike).
// Dev-mode builds (vite dev server on :5173, or build:dev) target the Worker
// on localhost:5000 directly.
// Deliberately not read from VITE_API_BASE_URL: bun auto-loads .env files into
// process.env, which outranks .env.production in Vite and once leaked the dev
// URL into a production build.
export const API_BASE_URL = import.meta.env.DEV
  ? 'http://localhost:5000/api'
  : '/api';
