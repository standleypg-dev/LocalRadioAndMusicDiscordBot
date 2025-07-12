import {DashboardApp} from "./src/views/dashboard/dashboard-app";
import {LoadingSpinner} from "./src/components/loading-spinner/loading-spinner";

export {};

declare global {
    interface HTMLElementTagNameMap {
        'dashboard-app': DashboardApp;
        'loading-spinner': LoadingSpinner;
    }
}