import {css, html, LitElement} from "lit";
import {customElement, property} from "lit/decorators.js";
import {commonStyles} from "../../styles/common.ts";

@customElement('app-error')
export class AppError extends LitElement {
    static styles = [
        commonStyles,
        css`
            .error {
                display: flex;
                flex-direction: column;
                align-items: center;
                justify-content: center;
                color: #fb8997;
                font-family: Arial, sans-serif;
            }`
    ];
    @property({type: String}) message = 'An unexpected error occurred.';

    render() {
        return html`
            <div class="glass-card error">
                <h3>Error</h3>
                <p>${this.message}</p>
            </div>
        `;
    }
}