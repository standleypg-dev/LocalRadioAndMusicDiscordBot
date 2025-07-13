import {LitElement, html} from 'lit';
import {customElement, state} from 'lit/decorators.js';
import {RadioAdminStyles} from "./radio-admin.styles.ts";
import type {RadioSource} from "../../interfaces/common.interfaces.ts";
import {Task} from "@lit/task";
import {provide} from "@lit/context";
import {RadioSourceService, radioSourceServiceCtx} from "../../services/radio-source.service.ts";
import '../../components/loading-spinner/loading-spinner.ts'
import '../../components/error/app-error.ts'

@customElement('radio-admin')
export class RadioAdmin extends LitElement {
    @state() radioStations: RadioSource[] = [];
    @state() showAddForm = false;
    @state() editingStation: RadioSource | null = null;
    @provide({context: radioSourceServiceCtx}) radioSourceService = new RadioSourceService();

    static readonly styles = RadioAdminStyles;

    private readonly _radiosTask = new Task(this, {
        task: async () => {
            this.radioStations = await this.radioSourceService.loadRadioSources()
        },
        args: () => [],
    });

    private showAddModal() {
        this.showAddForm = true;
    }

    private hideModal() {
        this.showAddForm = false;
        this.editingStation = null;
    }

    private editStation(station: RadioSource) {
        this.editingStation = {...station};
        this.showAddForm = true;
    }

    private async deleteStation(stationId: string) {
        if (confirm('Are you sure you want to delete this radio station?')) {
            try {
                await this.radioSourceService.deleteRadioSource(stationId);
                this.radioStations = this.radioStations.filter(s => s.id !== stationId);
            } catch {
                alert('Failed to delete radio station. Please try again.');
                return;
            }
        }
    }

    private async saveStation(formData: FormData) {
        const name = formData.get('name') as string;
        const sourceUrl = formData.get('url') as string;
        const isActive = formData.get('isActive') === 'on';
        if (this.editingStation) {
            const id = this.editingStation.id;
            // Update existing station
            const idx = this.radioStations.findIndex(s => s.id === id);
            try {
                await this.radioSourceService.updateRadioSource(id, sourceUrl, isActive);
                this.radioStations[idx] = {
                    ...this.radioStations[idx],
                    name,
                    sourceUrl,
                    isActive
                };
            } catch {
                alert('Failed to update radio station. Please try again.');
                return;
            }
        } else {
            // Add new station
            const newStation = await this.radioSourceService.addRadioSource(name, sourceUrl);
            this.radioStations.push(newStation);
        }

        this.radioStations = [...this.radioStations];
        this.hideModal();
    }

    private async handleFormSubmit(e: Event) {
        e.preventDefault();
        const formData = new FormData(e.target as HTMLFormElement);
        await this.saveStation(formData);
    }

    render() {
        return this._radiosTask.render({
            pending: () => html`
                <loading-spinner/>`,
            complete: () => {
                const totalStations = this.radioStations.length;
                const activeStations = this.radioStations.filter(s => s.isActive).length;
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
                    </div>

                    <div class="content-card glass-card">
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
                                        <div class="radio-url" title="${station.sourceUrl}">${station.sourceUrl}</div>
                                    </div>
                                    <div class="radio-actions">
                                        <button class="action-button edit"
                                                @click=${() => this.editStation(station)}>
                                            Edit
                                        </button>
                                        <button class="action-button delete"
                                                @click=${() => this.deleteStation(station.id)}>
                                            Delete
                                        </button>
                                    </div>
                                </div>
                            `)}
                        </div>
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
                                                .value=${this.editingStation?.name ?? ''}
                                        />
                                    </div>
                                    <div class="form-group">
                                        <label class="form-label">Stream URL</label>
                                        <input
                                                type="url"
                                                name="url"
                                                class="form-input"
                                                required
                                                .value=${this.editingStation?.sourceUrl ?? ''}
                                        />
                                    </div>
                                    <div class="form-group">
                                        <label class="form-checkbox">
                                            <input
                                                    type="checkbox"
                                                    name="isActive"
                                                    ?checked=${this.editingStation?.isActive !== false}
                                                    ?disabled=${this.editingStation === null}
                                            />
                                            <span>Station is active</span>
                                        </label>
                                    </div>
                                    <div class="form-actions">
                                        <button type="button" class="form-button glass-card secondary"
                                                @click=${this.hideModal}>
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
            },
            error: (e) => html`
                <app-error message=${e}></app-error>
            `
        })
    }
}