import {LitElement, html, css} from 'lit';
import {customElement} from 'lit/decorators.js';

@customElement('login-page')
export class LoginPage extends LitElement {
    static readonly styles = css``;

    handleLogin() {
        // Simulate login
        // In a real application, you would handle authentication here
        localStorage.setItem('authToken', 'some-token');
        window.location.href = '/';
    }

    render() {
        return html`
      <div class="login-form">
        <h2>Login</h2>
        <button @click=${this.handleLogin}>Login</button>
      </div>
    `;
    }
}
