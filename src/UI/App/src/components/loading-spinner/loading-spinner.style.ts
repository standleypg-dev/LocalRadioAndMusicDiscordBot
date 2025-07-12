import {commonStyles} from "../../styles/common.ts";
import {css} from "lit";

export const LoadingSpinnerStyles = [commonStyles, css`
        .spinner {
            width: 100px;
            height: 100px;
            border-radius: 50%;
            display: inline-block;
            border-top: 3px solid #FFF;
            border-right: 3px solid transparent;
            box-sizing: border-box;
            animation: rotation 1s linear infinite;
        }

        @keyframes rotation {
            0% {
                transform: rotate(0deg);
            }
            100% {
                transform: rotate(360deg);
            }
        }
    `]