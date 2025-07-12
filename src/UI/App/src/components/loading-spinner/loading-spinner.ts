import {LitElement, html} from 'lit';
import {customElement} from 'lit/decorators.js';
import {LoadingSpinnerStyles} from "./loading-spinner.style.ts";


@customElement('loading-spinner')
export class LoadingSpinner extends LitElement {
    static readonly styles = LoadingSpinnerStyles;

    render() {
        return html`
            <div class="loading">
                <span class="spinner"></span>
            </div>
        `;
    }
}
