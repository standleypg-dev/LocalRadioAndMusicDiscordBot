import {LitElement, html} from 'lit';
import {customElement, state} from 'lit/decorators.js';
import {Router} from '@vaadin/router';
import '../song/song-stats';
import '../user/user-stats';
import '../admin/radio-admin';
import '../login/login';
import {DashboardAppStyles} from "./dashboard-app.styles.ts";

@customElement('dashboard-app')
export class DashboardApp extends LitElement {
    static readonly styles = DashboardAppStyles;

    @state()
    private currentPath = window.location.pathname;

    isLoggedIn(): boolean {
        // Replace this with your real login logic (e.g., check token, session, etc.)
        return localStorage.getItem('authToken') !== null;
    }

    firstUpdated() {
        const router = new Router(this.renderRoot.querySelector('#outlet'));
        router.setRoutes([
            {path: '/', redirect: '/songs'},
            {path: '/songs', component: 'song-stats'},
            {path: '/users', component: 'user-stats'},
            {
                path: '/admin',
                component: 'radio-admin',
                action: async (_context, commands) => {
                    if (!this.isLoggedIn()) {
                        return commands.redirect('/login');
                    }
                },
            },
            {
                path: '/login',
                component: 'login-page'
            },
            {
                path: '(.*)',
                action: async () => {
                    // Handle 404 or redirect to home
                    console.warn('Page not found, redirecting to home');
                    Router.go('/');
                }
            }
        ]).catch((error) => {
            console.error('Router error:', error);
            Router.go('/login');
        });

        window.addEventListener('popstate', () => {
            this.currentPath = window.location.pathname;
        });

        const originalGo = Router.go;
        Router.go = (path: string) => {
            this.currentPath = path;
            return originalGo.call(Router, path);
        };
    }

    private isActive(path: string): string {
        return this.currentPath === path ? 'active' : '';
    }

    private handleNavigation(path: string) {
        this.currentPath = path;
        Router.go(path);
    }
    
    private handleLogout() {
        localStorage.removeItem('authToken');
        Router.go('/');
    }

    render() {
        return html`
            <div class="header-container">
                <div class="header-content">
                    <div class="logo">
                        <img class="logo-icon" src="/logo.png" alt="Logo"/>
                        <span>Rytho Dashboard</span>
                    </div>
                    <nav class="nav">
                        <button
                                class="nav-button glass-button ${this.isActive('/songs')}"
                                @click=${() => this.handleNavigation('/songs')}
                        >Song Statistics
                        </button>
                        <button
                                class="nav-button glass-button ${this.isActive('/users')}"
                                @click=${() => this.handleNavigation('/users')}
                        >User Statistics
                        </button>
                        ${this.isLoggedIn() ? html`
                            <button
                                    class="nav-button glass-button ${this.isActive('/admin')}"
                                    @click=${() => this.handleNavigation('/admin')}
                            >Radio Admin
                            </button>` : html``}
                        ${!this.isLoggedIn() ? html`
                            <div
                                    class="login ${this.isActive('/login')}"
                                    @click=${() => this.handleNavigation('/login')}
                            >
                                <img src="/log-in.svg" alt="Login" title="Login"/>
                            </div>` : html`
                            <div
                                    class="login"
                                    @click=${() => this.handleLogout()}
                            >
                                <img src="/log-out.svg" alt="Logout" title="Logout"/>
                            </div>`}
                    </nav>
                </div>
            </div>

            <main class="main">
                <div class="tab-content" id="outlet"></div>
            </main>
        `;
    }
}