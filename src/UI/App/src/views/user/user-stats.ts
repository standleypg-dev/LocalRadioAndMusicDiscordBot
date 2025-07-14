import {LitElement, html} from 'lit';
import {customElement, state} from 'lit/decorators.js';
import {Chart, registerables} from 'chart.js';
import {UserStatsStyles} from "./user-stats.styles.ts";
import {Task} from "@lit/task";
import {UserService, userServiceContext} from "../../services/user-service.ts";
import {provide} from '@lit/context';
import type {UserStatsDto} from "../../interfaces/common.interfaces.ts";
import '../../components/loading-spinner/loading-spinner.ts'
import '../../components/error/app-error.ts'

Chart.register(...registerables);

@customElement('user-stats')
export class UserStats extends LitElement {
    @provide({context: userServiceContext}) userService = new UserService();
    @state() viewMode: 'table' | 'chart' = 'table';
    @state() userStats: UserStatsDto[] = [];

    static readonly styles = UserStatsStyles;

    private readonly _usersTask = new Task(this, {
        task: async () => {
            this.userStats = await this.userService.loadUsers();
        },
        args: () => [],
    });

    private toggleView(mode: 'table' | 'chart') {
        this.viewMode = mode;
        if (mode === 'chart') {
            this.updateComplete.then(() => this.renderChart());
        }
    }

    private renderChart() {
        const canvas = this.shadowRoot?.querySelector('#usersChart') as HTMLCanvasElement;
        if (!canvas) return;

        const ctx = canvas.getContext('2d');
        if (!ctx) return;

        new Chart(ctx, {
            type: 'doughnut',
            data: {
                labels: this.userStats.slice(0, 8).map(user => user.username),
                datasets: [{
                    data: this.userStats.slice(0, 8).map(user => user.totalPlays),
                    backgroundColor: [
                        '#ff6b6b', '#4ecdc4', '#45b7d1', '#f9ca24', '#f0932b',
                        '#eb4d4b', '#6ab04c', '#9c88ff'
                    ],
                    borderWidth: 0,
                    hoverOffset: 10
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    legend: {
                        position: 'right',
                        labels: {
                            color: 'rgba(255, 255, 255, 0.8)',
                            usePointStyle: true,
                            padding: 20
                        }
                    }
                }
            }
        });
    }

    render() {
        return this._usersTask.render({
            pending: () => html`
                <loading-spinner/>`,
            complete: () => {
                const totalUsers = this.userStats.length;
                const totalPlays = this.userStats.reduce((sum, user) => sum + user.totalPlays, 0);
                const avgPlays = totalPlays > 0 ? Math.round(totalPlays / totalUsers) : 0;
                const topUser = this.userStats.reduce((max, user) => user.totalPlays > max.totalPlays ? user : max, this.userStats[0]);
                return html`
                    <div class="header">
                        <h1 class="title">User Statistics</h1>
                        <div class="view-toggle">
                            <button
                                    class="toggle-button ${this.viewMode === 'table' ? 'active' : ''}"
                                    @click=${() => this.toggleView('table')}
                            >
                                Table View
                            </button>
                            <button
                                    class="toggle-button ${this.viewMode === 'chart' ? 'active' : ''}"
                                    @click=${() => this.toggleView('chart')}
                            >
                                Chart View
                            </button>
                        </div>
                    </div>

                    <div class="stats-grid">
                        <div class="glass-card stat-card">
                            <h2 class="stat-value">${totalUsers}</h2>
                            <p class="stat-label">Active Users</p>
                        </div>
                        <div class="glass-card stat-card">
                            <h2 class="stat-value">${totalPlays.toLocaleString()}</h2>
                            <p class="stat-label">Total User Plays</p>
                        </div>
                        <div class="glass-card stat-card">
                            <h2 class="stat-value">${avgPlays.toLocaleString()}</h2>
                            <p class="stat-label">Average Plays per User</p>
                        </div>
                        <div class="glass-card stat-card">
                            <h2 class="stat-value">${topUser?.username || 'N/A'}</h2>
                            <p class="stat-label">Top User</p>
                        </div>
                    </div>

                    <div class="content-card">
                        ${this.viewMode === 'table' ? html`
                            <table class="table">
                                <thead>
                                <tr>
                                    <th>Rank</th>
                                    <th>User</th>
                                    <th>Total Plays</th>
                                    <th>Unique Songs</th>
                                    <th>Member Since</th>
                                    <th>Last Played</th>
                                </tr>
                                </thead>
                                <tbody>
                                ${this.userStats.slice(0, 10).map((user, index) => html`
                                    <tr>
                                        <td>${index + 1}</td>
                                        <td>
                                            <div class="user-info">
                                                <div class="user-avatar">${user.username.slice(0,2)}</div>
                                                <div class="user-details">
                                                    <div class="username">${user.username}</div>
                                                    <div class="discriminator">#${user.displayName}</div>
                                                </div>
                                            </div>
                                        </td>
                                        <td>
                                            <span class="play-count">${user.totalPlays}</span>
                                        </td>
                                        <td>
                                            <span class="unique-song">${user.uniqueSongs}</span>
                                        </td>
                                        <td>${user.memberSince.toLocaleString().split('T')[0]}</td>
                                        <td>${user.lastPlayed?.toLocaleString().split('T')[0]}</td>
                                    </tr>
                                `)}
                                </tbody>
                            </table>
                        ` : html`
                            <div class="chart-container">
                                <canvas id="usersChart"></canvas>
                            </div>
                        `}
                    </div>
                `;
            },
            error: (e) => html`
                <app-error message=${e}></app-error>
            `
        })


    }
}