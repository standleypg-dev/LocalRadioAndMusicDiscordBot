import {DetailedHTMLProps, HTMLAttributes} from "react";

export {};

declare global {
    namespace JSX {
        interface IntrinsicElements {
            'dashboard-app': DetailedHTMLProps<HTMLAttributes<HTMLElement>, HTMLElement>;
            'loading-spinner': DetailedHTMLProps<HTMLAttributes<HTMLElement>, HTMLElement>;
        }
    }
}