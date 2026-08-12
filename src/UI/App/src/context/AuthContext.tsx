import { useState, useEffect, useCallback, type ReactNode } from 'react';
import { AuthContext } from '../hooks/useAuth';
import { API_BASE_URL } from '../services/api';

export function AuthProvider({ children }: { children: ReactNode }) {
  const [isAuthenticated, setIsAuthenticated] = useState(false);

  const validateToken = useCallback(async () => {
    const token = localStorage.getItem('authToken');
    if (!token) {
      setIsAuthenticated(false);
      return false;
    }
    try {
      const response = await fetch(`${API_BASE_URL}/auth/validate-token`, {
        method: 'GET',
        headers: {
          Authorization: `Bearer ${token}`,
        },
      });
      if (!response.ok) {
        // Token is expired or invalid; drop it so route guards fail closed too.
        localStorage.removeItem('authToken');
      }
      setIsAuthenticated(response.ok);
      return response.ok;
    } catch {
      setIsAuthenticated(false);
      return false;
    }
  }, []);

  useEffect(() => {
    // eslint-disable-next-line react-hooks/set-state-in-effect
    validateToken();
  }, [validateToken]);

  const login = useCallback((token: string) => {
    localStorage.setItem('authToken', token);
    setIsAuthenticated(true);
  }, []);

  const logout = useCallback(() => {
    localStorage.removeItem('authToken');
    setIsAuthenticated(false);
  }, []);

  return (
    <AuthContext.Provider value={{ isAuthenticated, login, logout }}>
      {children}
    </AuthContext.Provider>
  );
}
