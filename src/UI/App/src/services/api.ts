// The production build uses a relative path because the Worker serves the SPA
// and the API from the same origin (works for local docker and deployment).
// The dev server overrides this via .env.development to reach localhost:5000.
export const API_BASE_URL = import.meta.env.VITE_API_BASE_URL ?? '/api';
