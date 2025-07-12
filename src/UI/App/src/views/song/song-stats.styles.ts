// file2-specific.js - Songs View
import { css } from 'lit';
import { commonStyles } from '../../styles/common';

export const SongStatsStyles = [
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

    .song-info {
      display: flex;
      flex-direction: column;
      gap: 0.25rem;
    }

    .song-title {
      font-weight: 600;
      color: rgba(255, 255, 255, 0.9);
    }

    .song-artist {
      font-size: 0.9rem;
      color: rgba(255, 255, 255, 0.6);
    }
  `
];