import { css } from 'lit';
import { commonStyles } from '../../styles/common';

export const DashboardAppStyles = [
    commonStyles,
    css`
        .header-container {
            background: rgba(0, 0, 0, 0.1);
            backdrop-filter: blur(40px) saturate(180%);
            -webkit-backdrop-filter: blur(40px) saturate(180%);
            border: 1px solid rgba(0, 0, 0, 0.2);
            border-bottom: 1px solid rgba(0, 0, 0, 0.1);
            padding: 1rem 2rem;
            position: sticky;
            top: 0;
            z-index: 100;
            box-shadow: 0 8px 32px rgba(0, 0, 0, 0.1);
        }

        .header-content {
            max-width: 1200px;
            margin: 0 auto;
            display: flex;
            align-items: center;
            justify-content: space-between;
        }

        .logo {
            display: flex;
            align-items: center;
            gap: 0.75rem;
            color: rgba(255, 255, 255, 0.9);
            font-size: 1.5rem;
            font-weight: 700;
        }

        .logo-icon {
            width: 32px;
            height: 32px;
            display: flex;
            align-items: center;
            justify-content: center;
            font-size: 1.2rem;
            color: rgba(255, 255, 255, 0.9);
            box-shadow: 0 4px 16px rgba(0, 0, 0, 0.1);
        }

        .nav {
            display: flex;
            gap: 0.5rem;
        }

        .nav-button {
            padding: 0.75rem 1.5rem;
            font-size: 0.9rem;
            font-weight: 500;
            position: relative;
            overflow: hidden;
        }

        .nav-button:hover {
            background: rgba(255, 255, 255, 0.15);
            border-color: rgba(255, 255, 255, 0.2);
            color: rgba(255, 255, 255, 0.9);
            transform: translateY(-1px);
            box-shadow: 0 4px 16px rgba(0, 0, 0, 0.1);
        }

        .nav-button.active {
            color: rgba(255, 255, 255, 1);
            background: rgba(255, 255, 255, 0.07);
            border-color: rgba(255, 255, 255, 0.3);
            box-shadow: 0 4px 20px rgba(0, 0, 0, 0.15);
        }

        .nav-button.active::before {
            content: '';
            position: absolute;
            bottom: 0;
            left: 50%;
            transform: translateX(-50%);
            width: 50%;
            height: 2px;
            background: rgba(255, 255, 255, 0.8);
            border-radius: 1px;
        }

        .main {
            max-width: 1200px;
            margin: 0 auto;
            padding: 2rem;
        }

        .tab-content {
            opacity: 0;
            animation: fadeIn 0.5s ease forwards;
        }

        @keyframes fadeIn {
            to {
                opacity: 1;
            }
        }

        @media (max-width: 768px) {
            .header {
                padding: 1rem;
            }

            .header-content {
                flex-direction: column;
                gap: 1rem;
            }

            .nav {
                flex-wrap: wrap;
                justify-content: center;
            }

            .main {
                padding: 1rem;
            }
        }
    `
];
