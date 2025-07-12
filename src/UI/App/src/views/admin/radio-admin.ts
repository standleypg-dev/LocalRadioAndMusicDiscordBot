import { LitElement, html } from 'lit';
import { customElement, state } from 'lit/decorators.js';
import {RadioAdminStyles} from "./radio-admin.styles.ts";

interface RadioStation {
    id: string;
    name: string;
    url: string;
    genre: string;
    country: string;
    isActive: boolean;
    listeners: number;
    description: string;
}

@customElement('radio-admin')
export class RadioAdmin extends LitElement {
    @state() radioStations: RadioStation[] = [];
    @state() loading = true;
    @state() showAddForm = false;
    @state() editingStation: RadioStation | null = null;

    static readonly styles = RadioAdminStyles;

    connectedCallback() {
        super.connectedCallback();
        this.loadRadioStations().catch(error => {
            console.error('Error loading radio stations:', error);
            this.loading = false;
        });
    }

    private async loadRadioStations() {
        this.radioStations = [
            {
                id: '1',
                name: 'Rock FM',
                url: 'http://stream.rockfm.com:8000/stream',
                genre: 'Rock',
                country: 'USA',
                isActive: true,
                listeners: 1247,
                description: 'Classic and modern rock hits 24/7'
            },
            {
                id: '2',
                name: 'Jazz Lounge',
                url: 'http://jazz.streamserver.com:8080/live',
                genre: 'Jazz',
                country: 'France',
                isActive: true,
                listeners: 892,
                description: 'Smooth jazz and classic instrumentals'
            },
            {
                id: '3',
                name: 'Pop Central',
                url: 'http://pop.radio.net:9000/stream',
                genre: 'Pop',
                country: 'UK',
                isActive: false,
                listeners: 2156,
                description: 'Latest pop hits and chart toppers'
            },
            {
                id: '4',
                name: 'Electronic Beats',
                url: 'http://edm.station.com:8000/live',
                genre: 'Electronic',
                country: 'Germany',
                isActive: true,
                listeners: 1634,
                description: 'EDM, techno, and electronic music'
            },
            {
                id: '5',
                name: 'Classical Harmony',
                url: 'http://classical.stream.org:8080/high',
                genre: 'Classical',
                country: 'Austria',
                isActive: true,
                listeners: 567,
                description: 'Beautiful classical compositions'
            },
            {
                id: '6',
                name: 'Hip-Hop Nation',
                url: 'http://hiphop.radio.com:8000/stream',
                genre: 'Hip-Hop',
                country: 'USA',
                isActive: false,
                listeners: 1823,
                description: 'Latest hip-hop and rap music'
            }
        ];

        this.loading = false;
        await this.updateComplete;
    }

    private showAddModal() {
        this.showAddForm = true;
    }

    private hideModal() {
        this.showAddForm = false;
        this.editingStation = null;
    }

    private editStation(station: RadioStation) {
        this.editingStation = { ...station };
        this.showAddForm = true;
    }

    private async deleteStation(stationId: string) {
        if (confirm('Are you sure you want to delete this radio station?')) {
            // Simulate API call
            this.radioStations = this.radioStations.filter(s => s.id !== stationId);
        }
    }

    private async saveStation(formData: FormData) {
        // Simulate API call
        const station: RadioStation = {
            id: this.editingStation?.id || Date.now().toString(),
            name: formData.get('name') as string,
            url: formData.get('url') as string,
            genre: formData.get('genre') as string,
            country: formData.get('country') as string,
            isActive: formData.get('isActive') === 'on',
            listeners: parseInt(formData.get('listeners') as string) || 0,
            description: formData.get('description') as string
        };

        if (this.editingStation) {
            // Update existing station
            const index = this.radioStations.findIndex(s => s.id === this.editingStation!.id);
            this.radioStations[index] = station;
        } else {
            // Add new station
            this.radioStations.push(station);
        }

        this.radioStations = [...this.radioStations];
        this.hideModal();
    }

    private handleFormSubmit(e: Event) {
        e.preventDefault();
        const formData = new FormData(e.target as HTMLFormElement);
        this.saveStation(formData);
    }

    render() {
        const totalStations = this.radioStations.length;
        const activeStations = this.radioStations.filter(s => s.isActive).length;
        const totalListeners = this.radioStations.reduce((sum, station) => sum + station.listeners, 0);

        return html`
            <div class="header">
                <h1 class="title">Radio Station Management</h1>
                <button class="add-button glass-card" @click=${this.showAddModal}>
                    Add New Station
                </button>
            </div>

            <div class="stats-grid">
                <div class="stat-card glass-card">
                    <h2 class="stat-value">${totalStations}</h2>
                    <p class="stat-label">Total Stations</p>
                </div>
                <div class="stat-card glass-card">
                    <h2 class="stat-value">${activeStations}</h2>
                    <p class="stat-label">Active Stations</p>
                </div>
                <div class="stat-card glass-card">
                    <h2 class="stat-value">${totalListeners.toLocaleString()}</h2>
                    <p class="stat-label">Total Listeners</p>
                </div>
                <div class="stat-card glass-card">
                    <h2 class="stat-value">${Math.round(totalListeners / Math.max(activeStations, 1))}</h2>
                    <p class="stat-label">Avg Listeners</p>
                </div>
            </div>

            <div class="content-card glass-card">
                ${this.loading ? html`
                    <div class="loading">Loading radio stations...</div>
                ` : html`
                    <div class="radio-grid">
                        ${this.radioStations.map(station => html`
                            <div class="radio-card">
                                <div class="radio-header">
                                    <h3 class="radio-name">${station.name}</h3>
                                    <span class="radio-status ${station.isActive ? 'active' : 'inactive'}">
                    ${station.isActive ? 'Active' : 'Inactive'}
                  </span>
                                </div>
                                <div class="radio-info">
                                    <p><strong>Genre:</strong> ${station.genre}</p>
                                    <p><strong>Country:</strong> ${station.country}</p>
                                    <p><strong>Listeners:</strong> ${station.listeners.toLocaleString()}</p>
                                    <p><strong>Description:</strong> ${station.description}</p>
                                    <div class="radio-url">${station.url}</div>
                                </div>
                                <div class="radio-actions">
                                    <button class="action-button edit" @click=${() => this.editStation(station)}>
                                        Edit
                                    </button>
                                    <button class="action-button delete" @click=${() => this.deleteStation(station.id)}>
                                        Delete
                                    </button>
                                </div>
                            </div>
                        `)}
                    </div>
                `}
            </div>

            ${this.showAddForm ? html`
                <div class="modal" @click=${(e: Event) => e.target === e.currentTarget && this.hideModal()}>
                    <div class="modal-content">
                        <div class="modal-header">
                            <h2 class="modal-title">
                                ${this.editingStation ? 'Edit Radio Station' : 'Add New Radio Station'}
                            </h2>
                            <button class="close-button" @click=${this.hideModal}>×</button>
                        </div>
                        <form @submit=${this.handleFormSubmit}>
                            <div class="form-group">
                                <label class="form-label">Station Name</label>
                                <input
                                        type="text"
                                        name="name"
                                        class="form-input"
                                        required
                                        .value=${this.editingStation?.name || ''}
                                />
                            </div>
                            <div class="form-group">
                                <label class="form-label">Stream URL</label>
                                <input
                                        type="url"
                                        name="url"
                                        class="form-input"
                                        required
                                        .value=${this.editingStation?.url || ''}
                                />
                            </div>
                            <div class="form-group">
                                <label class="form-label">Genre</label>
                                <select name="genre" class="form-select" required>
                                    <option value="">Select Genre</option>
                                    <option value="Rock" ?selected=${this.editingStation?.genre === 'Rock'}>Rock</option>
                                    <option value="Pop" ?selected=${this.editingStation?.genre === 'Pop'}>Pop</option>
                                    <option value="Jazz" ?selected=${this.editingStation?.genre === 'Jazz'}>Jazz</option>
                                    <option value="Classical" ?selected=${this.editingStation?.genre === 'Classical'}>Classical</option>
                                    <option value="Electronic" ?selected=${this.editingStation?.genre === 'Electronic'}>Electronic</option>
                                    <option value="Hip-Hop" ?selected=${this.editingStation?.genre === 'Hip-Hop'}>Hip-Hop</option>
                                    <option value="Country" ?selected=${this.editingStation?.genre === 'Country'}>Country</option>
                                    <option value="Alternative" ?selected=${this.editingStation?.genre === 'Alternative'}>Alternative</option>
                                </select>
                            </div>
                            <div class="form-group">
                                <label class="form-label">Country</label>
                                <input
                                        type="text"
                                        name="country"
                                        class="form-input"
                                        required
                                        .value=${this.editingStation?.country || ''}
                                />
                            </div>
                            <div class="form-group">
                                <label class="form-label">Current Listeners</label>
                                <input
                                        type="number"
                                        name="listeners"
                                        class="form-input"
                                        min="0"
                                        .value=${this.editingStation?.listeners?.toString() || '0'}
                                />
                            </div>
                            <div class="form-group">
                                <label class="form-label">Description</label>
                                <textarea
                                        name="description"
                                        class="form-textarea"
                                        .value=${this.editingStation?.description || ''}
                                ></textarea>
                            </div>
                            <div class="form-group">
                                <label class="form-checkbox">
                                    <input
                                            type="checkbox"
                                            name="isActive"
                                            ?checked=${this.editingStation?.isActive !== false}
                                    />
                                    <span>Station is active</span>
                                </label>
                            </div>
                            <div class="form-actions">
                                <button type="button" class="form-button glass-card secondary" @click=${this.hideModal}>
                                    Cancel
                                </button>
                                <button type="submit" class="form-button glass-card">
                                    ${this.editingStation ? 'Update Station' : 'Add Station'}
                                </button>
                            </div>
                        </form>
                    </div>
                </div>
            ` : ''}
        `;
    }
}