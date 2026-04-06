import { useState } from 'react';
import { useNavigate } from '@tanstack/react-router';
import { loginUser } from '../services/user-service';
import { useAuth } from '../hooks/useAuth';

export function Login() {
  const [userName, setUserName] = useState('');
  const [password, setPassword] = useState('');
  const { login } = useAuth();
  const navigate = useNavigate();

  async function handleLogin(e: React.FormEvent) {
    e.preventDefault();
    if (userName && password) {
      try {
        const { token } = await loginUser(userName, password);
        login(token);
        navigate({ to: '/' });
      } catch {
        alert('Login failed. Please check your credentials.');
      }
    } else {
      alert('Username and password cannot be empty.');
    }
  }

  return (
    <div className="modal" onClick={(e) => e.target === e.currentTarget}>
      <div className="modal-content">
        <div className="modal-header">
          <h2 className="modal-title">Login</h2>
        </div>
        <form onSubmit={handleLogin}>
          <div className="form-group">
            <label className="form-label">Username</label>
            <input
              type="text"
              name="username"
              className="form-input"
              required
              value={userName}
              onChange={(e) => setUserName(e.target.value)}
            />
          </div>
          <div className="form-group">
            <label className="form-label">Password</label>
            <input
              type="password"
              name="password"
              className="form-input"
              required
              value={password}
              onChange={(e) => setPassword(e.target.value)}
            />
          </div>
          <div className="form-actions">
            <button
              type="submit"
              className="form-button glass-card"
              style={{ color: 'white' }}
            >
              Login
            </button>
          </div>
        </form>
      </div>
    </div>
  );
}
