import {css} from 'lit';

export const commonStyles = css`
    .header {
        display: flex;
        justify-content: space-between;
        align-items: center;
        margin-bottom: 2rem;
    }

    .title {
        color: rgba(255, 255, 255, 0.9);
        font-size: 2rem;
        font-weight: 700;
        margin: 0;
        text-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
    }

    .glass-card {
        background: rgba(0, 0, 0, 0.1);
        backdrop-filter: blur(40px) saturate(180%);
        -webkit-backdrop-filter: blur(40px) saturate(180%);
        border: 1px solid rgba(255, 255, 255, 0.2);
        border-radius: 1rem;
        box-shadow: 0 8px 32px rgba(0, 0, 0, 0.1);
        transition: all 0.3s ease;
        
        &:hover {
            box-shadow: 0 16px 40px rgba(0, 0, 0, 0.2);
            background: rgba(0, 0, 0, 0.15);
            border-color: rgba(255, 255, 255, 0.3);
        }
    }
    
    .stats-grid {
        display: grid;
        gap: 1.5rem;
        margin-bottom: 2rem;
    }

    .stat-card {
        padding: 1.5rem;
        
        &:hover {
            transform: translateY(-5px);
            box-shadow: 0 16px 40px rgba(0, 0, 0, 0.2);
            background: rgba(255, 255, 255, 0.15);
            border-color: rgba(255, 255, 255, 0.3);
        }
    }

    .stat-value,
    .stat-song-title {
        color: rgba(255, 255, 255, 1);
        margin: 0;
        text-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
    }
    
    .stat-value {
        font-size: 2rem;
        font-weight: 700;
    }

    .stat-label {
        color: rgba(255, 255, 255, 0.7);
        font-size: 0.9rem;
        margin: 0.5rem 0 0 0;
    }

    .content-card {
        padding: 2rem;
        min-height: 400px;
    }

    .glass-button {
        background: rgba(0, 0, 0, 0.2);
        backdrop-filter: blur(20px);
        color: rgba(255, 255, 255, 0.9);
        border: 1px solid rgba(255, 255, 255, 0.3);
        border-radius: 50px;
        font-weight: 600;
        cursor: pointer;
        transition: all 0.3s ease;
        box-shadow: 0 4px 16px rgba(0, 0, 0, 0.1);
        
        &:hover {
            transform: translateY(-2px);
            box-shadow: 0 8px 24px rgba(0, 0, 0, 0.15);
            background: rgba(0, 0, 0, 0.25);
            border-color: rgba(0, 0, 0, 0.4);
        }
    }

    .view-toggle {
        display: flex;
        background: rgba(255, 255, 255, 0.1);
        backdrop-filter: blur(20px);
        border: 1px solid rgba(255, 255, 255, 0.2);
        border-radius: 50px;
        padding: 0.25rem;
        box-shadow: 0 4px 16px rgba(0, 0, 0, 0.1);
    }

    .toggle-button {
        background: none;
        border: none;
        color: rgba(255, 255, 255, 0.7);
        padding: 0.5rem 1rem;
        border-radius: 50px;
        cursor: pointer;
        font-size: 0.9rem;
        transition: all 0.3s ease;

        &.active {
            background: rgba(255, 255, 255, 0.2);
            color: rgba(255, 255, 255, 1);
            box-shadow: 0 4px 16px rgba(0, 0, 0, 0.15);
            backdrop-filter: blur(20px);
        }
    }
    
    .table {
        width: 100%;
        border-collapse: collapse;

        th,
        td {
            text-align: left;
            padding: 1rem;
            border-bottom: 1px solid rgba(255, 255, 255, 0.1);
        }

        th {
            color: rgba(255, 255, 255, 0.9);
            font-weight: 600;
            font-size: 0.9rem;
            text-transform: uppercase;
            letter-spacing: 0.05em;
        }

        td {
            color: rgba(255, 255, 255, 0.8);

            &:first-child {
                width: 10%;
            }
        }

        tr {
            transition: background 0.5s ease;
            
            &:hover {
                background: rgba(255, 255, 255, 0.05);
            }
        }
    }
    
    .play-count {
        background: rgba(255, 255, 255, 0.2);
        backdrop-filter: blur(20px);
        color: rgba(255, 255, 255, 0.9);
        border: 1px solid rgba(255, 255, 255, 0.3);
        padding: 0.25rem 0.75rem;
        border-radius: 50px;
        font-size: 0.8rem;
        font-weight: 600;
        display: inline-block;
        box-shadow: 0 2px 8px rgba(0, 0, 0, 0.1);
    }

    .chart-container {
        position: relative;
        height: 400px;
        width: 100%;
    }

    .loading {
        display: flex;
        justify-content: center;
        align-items: center;
        height: 50vh;
        color: rgba(255, 255, 255, 0.7);
        font-size: 1.1rem;
    }

    .form-group {
        margin-bottom: 1.5rem;
    }

    .form-label {
        display: block;
        color: rgba(255, 255, 255, 0.9);
        font-size: 0.9rem;
        font-weight: 500;
        margin-bottom: 0.5rem;
    }

    .form-input,
    .form-textarea,
    .form-select {
        width: 100%;
        background: rgba(255, 255, 255, 0.1);
        backdrop-filter: blur(20px);
        border: 1px solid rgba(255, 255, 255, 0.2);
        color: rgba(255, 255, 255, 0.9);
        padding: 0.75rem 0;
        border-radius: 0.5rem;
        font-size: 0.9rem;
        transition: all 0.3s ease;
        box-shadow: 0 2px 8px rgba(0, 0, 0, 0.05);
        
        &:focus {
            outline: none;
            border-color: rgba(255, 255, 255, 0.4);
            box-shadow: 0 0 0 3px rgba(255, 255, 255, 0.1);
            background: rgba(255, 255, 255, 0.15);
        }
    }
    
    .form-textarea {
        resize: vertical;
        min-height: 80px;
    }

    .modal {
        position: fixed;
        top: 0;
        left: 0;
        width: 100%;
        height: 100%;
        background: rgba(0, 0, 0, 0.8);
        backdrop-filter: blur(10px);
        display: flex;
        align-items: center;
        justify-content: center;
        z-index: 1000;
    }

    .modal-content {
        background: rgba(255, 255, 255, 0.1);
        backdrop-filter: blur(40px) saturate(180%);
        -webkit-backdrop-filter: blur(40px) saturate(180%);
        border: 1px solid rgba(255, 255, 255, 0.2);
        border-radius: 1rem;
        padding: 2rem;
        max-width: 500px;
        width: 90%;
        max-height: 80vh;
        overflow-y: auto;
        box-shadow: 0 20px 60px rgba(0, 0, 0, 0.3);
    }

    .modal-title {
        color: rgba(255, 255, 255, 0.9);
        font-size: 1.5rem;
        font-weight: 600;
        margin: 0;
    }

    .close-button {
        background: none;
        border: none;
        color: rgba(255, 255, 255, 0.7);
        font-size: 1.5rem;
        cursor: pointer;
        transition: all 0.3s ease;
        
        &:hover {
            color: rgba(255, 255, 255, 0.9);
        }
    }
    
    @media (max-width: 768px) {
        .header {
            flex-direction: column;
            gap: 1rem;
            align-items: stretch;
        }

        .stats-grid {
            grid-template-columns: 1fr;
        }

        .table {
            font-size: 0.9rem;

            td {
                padding: 0.75rem 0.5rem;
            }
        }
    }
`;