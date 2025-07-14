import {LitElement, html} from 'lit';
import {customElement, state} from 'lit/decorators.js';
import {provide} from "@lit/context";
import {UserService, userServiceContext} from "../../services/user-service.ts";
import {commonStyles} from "../../styles/common.ts";

@customElement('login-page')
export class LoginPage extends LitElement {
    @provide({context: userServiceContext}) userService = new UserService();
    static readonly styles = commonStyles;

    @state() userName = '';
    @state() password = '';

    async handleLogin(e: Event) {
        e.preventDefault();
        const formData = new FormData(e.target as HTMLFormElement);
        this.userName = formData.get('username') as string;
        this.password = formData.get('password') as string;
        if (this.userName && this.password) {
            try {
                const { token } = await this.userService.loginUser(this.userName, this.password);
                localStorage.setItem('authToken', token);
                window.location.href = '/';
            } catch {
                alert("Login failed. Please check your credentials.");
            }
        } else {
            alert("Username and password cannot be empty.");
        }
    }

    render() {
        return html`
            <div class="modal" @click=${(e: Event) => e.target === e.currentTarget}>
                <div class="modal-content">
                    <div class="modal-header">
                        <h2 class="modal-title">
                            Login
                        </h2>
                    </div>
                    <form @submit=${this.handleLogin}>
                        <div class="form-group">
                            <label class="form-label">Username</label>
                            <input
                                    type="text"
                                    name="username"
                                    class="form-input"
                                    required
                                    .value=${this.userName ?? ''}
                            />
                        </div>
                        <div class="form-group">
                            <label class="form-label">Password</label>
                            <input
                                    type="password"
                                    name="password"
                                    class="form-input"
                                    required
                                    .value=${this.password ?? ''}
                            />
                        </div>
                        <div class="form-actions">
                            <button type="submit" class="form-button glass-card" style="color: white">
                                Login
                            </button>
                        </div>
                    </form>
                </div>
            </div>
        `;

    }
}
