import { LitElement, html } from 'lit';
import { customElement, state } from 'lit/decorators.js';
import { Chart, registerables } from 'chart.js';
import {UserStatsStyles} from "./user-stats.styles.ts";

Chart.register(...registerables);

interface UserStat {
  id: string;
  username: string;
  discriminator: string;
  avatar: string;
  totalPlays: number;
  favoriteGenre: string;
  joinDate: string;
  lastActive: string;
}

@customElement('user-stats')
export class UserStats extends LitElement {
  @state() userStats: UserStat[] = [];
  @state() loading = true;
  @state() viewMode: 'table' | 'chart' = 'table';

  static styles = UserStatsStyles;

  async connectedCallback() {
    super.connectedCallback();
    await this.loadUserStats();
  }

  private async loadUserStats() {
    this.userStats = [
      { id: '1', username: 'MusicLover42', discriminator: '1234', avatar: 'ML', totalPlays: 2847, favoriteGenre: 'Rock', joinDate: '2022-01-15', lastActive: '2 hours ago' },
      { id: '2', username: 'RockFan88', discriminator: '5678', avatar: 'RF', totalPlays: 2156, favoriteGenre: 'Metal', joinDate: '2022-03-20', lastActive: '5 minutes ago' },
      { id: '3', username: 'PopPrincess', discriminator: '9012', avatar: 'PP', totalPlays: 1889, favoriteGenre: 'Pop', joinDate: '2022-05-10', lastActive: '1 hour ago' },
      { id: '4', username: 'JazzMaster', discriminator: '3456', avatar: 'JM', totalPlays: 1634, favoriteGenre: 'Jazz', joinDate: '2022-02-08', lastActive: '3 hours ago' },
      { id: '5', username: 'EDMVibes', discriminator: '7890', avatar: 'EV', totalPlays: 1523, favoriteGenre: 'Electronic', joinDate: '2022-04-12', lastActive: '15 minutes ago' },
      { id: '6', username: 'ClassicFan', discriminator: '2345', avatar: 'CF', totalPlays: 1456, favoriteGenre: 'Classical', joinDate: '2022-01-30', lastActive: '6 hours ago' },
      { id: '7', username: 'HipHopHead', discriminator: '6789', avatar: 'HH', totalPlays: 1389, favoriteGenre: 'Hip-Hop', joinDate: '2022-03-05', lastActive: '30 minutes ago' },
      { id: '8', username: 'FolkSoul', discriminator: '0123', avatar: 'FS', totalPlays: 1234, favoriteGenre: 'Folk', joinDate: '2022-06-18', lastActive: '4 hours ago' },
      { id: '9', username: 'Metalhead666', discriminator: '4567', avatar: 'MH', totalPlays: 1156, favoriteGenre: 'Metal', joinDate: '2022-02-22', lastActive: '1 day ago' },
      { id: '10', username: 'SynthWave80s', discriminator: '8901', avatar: 'SW', totalPlays: 1089, favoriteGenre: 'Synthwave', joinDate: '2022-05-30', lastActive: '2 days ago' },
    ];

    this.loading = false;
  }

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
    const totalUsers = this.userStats.length;
    const totalPlays = this.userStats.reduce((sum, user) => sum + user.totalPlays, 0);
    const avgPlays = totalPlays > 0 ? Math.round(totalPlays / totalUsers) : 0;
    const topUser = this.userStats[0];

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
        ${this.loading ? html`
          <div class="loading">Loading user statistics...</div>
        ` : html`
          ${this.viewMode === 'table' ? html`
            <table class="table">
              <thead>
              <tr>
                <th>Rank</th>
                <th>User</th>
                <th>Total Plays</th>
                <th>Favorite Genre</th>
                <th>Last Active</th>
              </tr>
              </thead>
              <tbody>
              ${this.userStats.map((user, index) => html`
                <tr>
                  <td>${index + 1}</td>
                  <td>
                    <div class="user-info">
                      <div class="user-avatar">${user.avatar}</div>
                      <div class="user-details">
                        <div class="username">${user.username}</div>
                        <div class="discriminator">#${user.discriminator}</div>
                      </div>
                    </div>
                  </td>
                  <td>
                    <span class="play-count">${user.totalPlays.toLocaleString()}</span>
                  </td>
                  <td>
                    <span class="genre-tag">${user.favoriteGenre}</span>
                  </td>
                  <td>${user.lastActive}</td>
                </tr>
              `)}
              </tbody>
            </table>
          ` : html`
            <div class="chart-container">
              <canvas id="usersChart"></canvas>
            </div>
          `}
        `}
      </div>
    `;
  }
}