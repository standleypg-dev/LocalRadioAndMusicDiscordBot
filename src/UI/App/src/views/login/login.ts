import {LitElement, html, css} from 'lit';
import {customElement} from 'lit/decorators.js';

@customElement('login-page')
export class LoginPage extends LitElement {
    static readonly styles = css`
    /* Your styles here */
  `;

    handleLogin() {
        // Simulate login
        localStorage.setItem('authToken', 'some-token');
        window.location.href = '/admin'; // redirect manually after login
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
