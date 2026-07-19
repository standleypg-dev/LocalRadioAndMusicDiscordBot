import { Outlet, useNavigate, useRouterState } from '@tanstack/react-router';
import { useAuth } from '../hooks/useAuth';

export function DashboardLayout() {
  const navigate = useNavigate();
  const { isAuthenticated, logout } = useAuth();
  const routerState = useRouterState();
  const currentPath = routerState.location.pathname;

  function isActive(path: string): string {
    return currentPath === path ? 'active' : '';
  }

  function handleNavigation(path: string) {
    navigate({ to: path });
  }

  function handleLogout() {
    logout();
    navigate({ to: '/' });
  }

  return (
    <>
      <div className="header-container">
        <div className="header-content">
          <div className="logo">
            <img className="logo-icon" src="/logo.png" alt="Logo" />
            <span>Rytho Dashboard</span>
          </div>
          <nav className="nav">
            <button
              className={`nav-button glass-button ${isActive('/')}`}
              onClick={() => handleNavigation('/')}
            >
              Overview
            </button>
            <button
              className={`nav-button glass-button ${isActive('/users')}`}
              onClick={() => handleNavigation('/users')}
            >
              User Statistics
            </button>
            <button
              className={`nav-button glass-button ${isActive('/songs')}`}
              onClick={() => handleNavigation('/songs')}
            >
              Song Statistics
            </button>
            {isAuthenticated && (
              <button
                className={`nav-button glass-button ${isActive('/admin')}`}
                onClick={() => handleNavigation('/admin')}
              >
                Radio Admin
              </button>
            )}
          </nav>
          <div className="header-actions">
            {!isAuthenticated ? (
              <div
                className={`login-icon ${isActive('/login')}`}
                onClick={() => handleNavigation('/login')}
              >
                <img src="/log-in.svg" alt="Login" title="Login" />
              </div>
            ) : (
              <div className="login-icon" onClick={handleLogout}>
                <img src="/log-out.svg" alt="Logout" title="Logout" />
              </div>
            )}
          </div>
        </div>
      </div>

      <main className="main">
        <div className="tab-content">
          <Outlet />
        </div>
      </main>
    </>
  );
}
