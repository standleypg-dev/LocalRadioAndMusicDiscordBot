// file1-specific.js - Users View
import { css } from 'lit';
import { commonStyles } from '../../styles/common';

export const UserStatsStyles = [
    commonStyles,
    css`
        .stats-grid {
            grid-template-columns: repeat(auto-fit, minmax(250px, 1fr));
        }

        .stat-card {
            /* Uses .glass-card base styles */
        }

        .content-card {
            /* Uses .glass-card base styles */
        }

        .user-info {
            display: flex;
            align-items: center;
            gap: 1rem;
        }

        .user-avatar {
            width: 40px;
            height: 40px;
            border-radius: 50%;
            background: rgba(255, 255, 255, 0.2);
            backdrop-filter: blur(20px);
            border: 1px solid rgba(255, 255, 255, 0.3);
            display: flex;
            align-items: center;
            justify-content: center;
            color: rgba(255, 255, 255, 0.9);
            font-weight: 600;
            font-size: 1.2rem;
            box-shadow: 0 4px 16px rgba(0, 0, 0, 0.1);
        }

        .user-details {
            display: flex;
            flex-direction: column;
            gap: 0.25rem;
        }

        .username {
            font-weight: 600;
            color: rgba(255, 255, 255, 0.9);
        }

        .discriminator {
            font-size: 0.8rem;
            color: rgba(255, 255, 255, 0.5);
        }

        .unique-song {
            background: rgba(255, 255, 255, 0.1);
            backdrop-filter: blur(20px);
            color: rgba(255, 255, 255, 0.8);
            border: 1px solid rgba(255, 255, 255, 0.2);
            padding: 0.25rem 0.75rem;
            border-radius: 50px;
            font-size: 0.8rem;
            display: inline-block;
            box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
        }

        @media (max-width: 768px) {
            .user-info {
                gap: 0.5rem;
            }

            .user-avatar {
                width: 32px;
                height: 32px;
                font-size: 1rem;
            }
        }
    `
];