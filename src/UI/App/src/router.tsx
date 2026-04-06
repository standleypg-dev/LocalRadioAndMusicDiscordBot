import {
  createRouter,
  createRootRoute,
  createRoute,
  redirect,
} from '@tanstack/react-router';
import { DashboardLayout } from './layouts/DashboardLayout';
import { SongStats } from './pages/SongStats';
import { UserStats } from './pages/UserStats';
import { RadioAdmin } from './pages/RadioAdmin';
import { Login } from './pages/Login';

const rootRoute = createRootRoute({
  component: DashboardLayout,
});

const indexRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: '/',
  beforeLoad: () => {
    throw redirect({ to: '/songs' });
  },
});

const songsRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: '/songs',
  component: SongStats,
});

const usersRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: '/users',
  component: UserStats,
});

const adminRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: '/admin',
  beforeLoad: () => {
    const token = localStorage.getItem('authToken');
    if (!token) {
      throw redirect({ to: '/login' });
    }
  },
  component: RadioAdmin,
});

const loginRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: '/login',
  component: Login,
});

const catchAllRoute = createRoute({
  getParentRoute: () => rootRoute,
  path: '$',
  beforeLoad: () => {
    throw redirect({ to: '/songs' });
  },
});

const routeTree = rootRoute.addChildren([
  indexRoute,
  songsRoute,
  usersRoute,
  adminRoute,
  loginRoute,
  catchAllRoute,
]);

export const router = createRouter({ routeTree });

declare module '@tanstack/react-router' {
  interface Register {
    router: typeof router;
  }
}
