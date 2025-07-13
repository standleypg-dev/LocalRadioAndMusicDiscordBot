import {html, LitElement} from 'lit';
import {customElement, state} from 'lit/decorators.js';
import {Chart, registerables} from 'chart.js';
import {SongStatsStyles} from "./song-stats.styles.ts";
import {Task} from '@lit/task';
import {provide} from '@lit/context';
import type { SongStat } from '../../interfaces/common.interfaces.ts';
import {SongStatsService, songStatsServiceCtx} from "../../services/song-stats.service.ts";
import '../../components/loading-spinner/loading-spinner.ts'
import '../../components/error/app-error.ts'

Chart.register(...registerables);

@customElement('song-stats')
export class SongStats extends LitElement {
    @provide({context: songStatsServiceCtx}) songStatsService = new SongStatsService();
    @state() viewMode: 'table' | 'chart' = 'table';

    static readonly styles = SongStatsStyles;

    private readonly _songStatsTask = new Task(this, {
        task: async () => await this.songStatsService.loadSongStats(),
        args: () => [],
    });

    private toggleView(mode: 'table' | 'chart', songs: SongStat[]) {
        this.viewMode = mode;
        if (mode === 'chart') {
            this.updateComplete.then(() => this.renderChart(songs));
        }
    }

    private renderChart(songs: SongStat[]) {
        const canvas = this.shadowRoot?.querySelector('#songsChart') as HTMLCanvasElement;
        if (!canvas) return;

        const ctx = canvas.getContext('2d');
        if (!ctx) return;

        new Chart(ctx, {
            type: 'bar',
            data: {
                labels: songs.slice(0, 10).map(song => `${this.songStatsService.cleanTitle(song.title)} ${song.artist ? `- ${song.artist}` : ''}`),
                datasets: [{
                    label: 'Play Count',
                    data: songs.slice(0, 10).map(song => song.playCount),
                    backgroundColor: 'rgba(78, 205, 196, 0.8)',
                    borderColor: 'rgba(78, 205, 196, 1)',
                    borderWidth: 2,
                    borderRadius: 8,
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    legend: {
                        display: false
                    }
                },
                scales: {
                    y: {
                        beginAtZero: true,
                        grid: {
                            color: 'rgba(255, 255, 255, 0.1)'
                        },
                        ticks: {
                            color: 'rgba(255, 255, 255, 0.7)'
                        }
                    },
                    x: {
                        grid: {
                            display: false
                        },
                        ticks: {
                            color: 'rgba(255, 255, 255, 0.7)',
                            maxRotation: 45,
                            minRotation: 45
                        }
                    }
                }
            }
        });
    }

    render() {
        return this._songStatsTask.render({
            pending: () => html`
                <loading-spinner/>`,
            complete: (songs) => {
                const totalPlays = songs.reduce((sum, song) => sum + song.playCount, 0);
                const avgPlays = totalPlays > 0 ? Math.round(totalPlays / songs.length) : 0;
                const topSong = songs[0];
                return html`
                    <div class="header">
                        <h1 class="title">Song Statistics</h1>
                        <div class="view-toggle">
                            <button
                                    class="toggle-button ${this.viewMode === 'table' ? 'active' : ''}"
                                    @click=${() => this.toggleView('table', songs)}
                            >
                                Table View
                            </button>
                            <button
                                    class="toggle-button ${this.viewMode === 'chart' ? 'active' : ''}"
                                    @click=${() => this.toggleView('chart', songs)}
                            >
                                Chart View
                            </button>
                        </div>
                    </div>

                    <div class="stats-grid">
                        <div class="glass-card stat-card">
                            <h2 class="stat-value">${totalPlays.toLocaleString()}</h2>
                            <p class="stat-label">Total Plays</p>
                        </div>
                        <div class="glass-card stat-card">
                            <h2 class="stat-value">${songs.length}</h2>
                            <p class="stat-label">Unique Songs</p>
                        </div>
                        <div class="glass-card stat-card">
                            <h2 class="stat-value">${avgPlays.toLocaleString()}</h2>
                            <p class="stat-label">Average Plays</p>
                        </div>
                        <div class="glass-card stat-card">
                            <h3 class="stat-song-title">${this.songStatsService.cleanTitle(topSong?.title) || 'N/A'}</h3>
                            <p class="stat-label">Most Played Song</p>
                        </div>
                    </div>

                    <div class="content-card">
                        ${this.viewMode === 'table' ? html`
                            <table class="table">
                                <thead>
                                <tr>
                                    <th>Rank</th>
                                    <th>Song</th>
                                    <th>Plays</th>
                                </tr>
                                </thead>
                                <tbody>
                                ${songs.slice(0, 10).map((song, index) => html`
                                    <tr>
                                        <td>${index + 1}</td>
                                        <td>
                                            <div class="song-info">
                                                <div class="song-title">${this.songStatsService.cleanTitle(song.title)}</div>
                                                <div class="song-artist">${song.artist}</div>
                                            </div>
                                        </td>
                                        <td>
                                            <span class="play-count">${song.playCount.toLocaleString()}</span>
                                        </td>
                                    </tr>
                                `)}
                                </tbody>
                            </table>
                        ` : html`
                            <div class="chart-container">
                                <canvas id="songsChart"></canvas>
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