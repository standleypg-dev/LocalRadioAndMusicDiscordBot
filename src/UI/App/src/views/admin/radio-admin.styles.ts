// file3-specific.js - Radio Management
import {css} from 'lit';
import {commonStyles} from '../../styles/common';

export const RadioAdminStyles = [
    commonStyles,
    css`
        .stats-grid {
            grid-template-columns: repeat(auto-fit, minmax(200px, 1fr));
        }

        .content-card {
            margin-bottom: 2rem;
        }

        .add-button {
            padding: 0.75rem 1.5rem;
            font-size: 0.9rem;
            background: rgba(76, 175, 80, 0.3);
            color: white;
            
            &:hover {
                background: rgba(76, 175, 80, 0.8);
            }
        }
        
        .radio-grid {
            display: grid;
            grid-template-columns: repeat(auto-fit, minmax(350px, 1fr));
            gap: 1.5rem;
        }

        .radio-card {
            background: rgba(255, 255, 255, 0.05);
            backdrop-filter: blur(20px);
            border: 1px solid rgba(255, 255, 255, 0.15);
            border-radius: 1rem;
            padding: 1.5rem;
            transition: all 0.3s ease;
            box-shadow: 0 4px 16px rgba(0, 0, 0, 0.05);
            
            &:hover {
                background: rgba(255, 255, 255, 0.1);
                transform: translateY(-3px);
                box-shadow: 0 8px 24px rgba(0, 0, 0, 0.1);
                border-color: rgba(255, 255, 255, 0.2);
            }
        }
        
        .radio-header {
            display: flex;
            justify-content: space-between;
            align-items: flex-start;
            margin-bottom: 1rem;
        }

        .radio-name {
            font-size: 1.2rem;
            font-weight: 600;
            color: rgba(255, 255, 255, 0.9);
            margin: 0;
        }

        .radio-status {
            padding: 0.25rem 0.75rem;
            border-radius: 50px;
            font-size: 0.8rem;
            font-weight: 600;

            &.active {
                background: rgba(52, 199, 89, 0.7);
                color: rgba(255, 255, 255, 0.9);
                backdrop-filter: blur(20px);
                border: 1px solid rgba(52, 199, 89, 0.4);
            }

            &.inactive {
                background: rgba(255, 59, 48, 0.7);
                color: rgba(255, 255, 255, 0.9);
                backdrop-filter: blur(20px);
                border: 1px solid rgba(255, 59, 48, 0.4);
            }
        }

        .modal-header {
            display: flex;
            justify-content: space-between;
            align-items: center;
            margin-bottom: 2rem;
        }

        .radio-info {
            margin-bottom: 1rem;

            p {
                margin: 0.5rem 0;
                color: rgba(255, 255, 255, 0.8);
                font-size: 0.9rem;
            }
        }
        
        .radio-url {
            background: rgba(255, 255, 255, 0.1);
            backdrop-filter: blur(20px);
            border: 1px solid rgba(255, 255, 255, 0.2);
            padding: 0.5rem;
            border-radius: 0.5rem;
            font-family: monospace;
            font-size: 0.8rem;
            color: rgba(255, 255, 255, 0.9);
            word-break: break-all;
            box-shadow: 0 2px 8px rgba(0, 0, 0, 0.05);
        }

        .radio-actions {
            display: flex;
            gap: 0.5rem;
            margin-top: 1rem;
        }

        .action-button {
            background: rgba(255, 255, 255, 0.5);
            backdrop-filter: blur(20px);
            border: 1px solid rgba(255, 255, 255, 0.2);
            padding: 0.5rem 1rem;
            border-radius: 0.5rem;
            font-size: 0.8rem;
            cursor: pointer;
            transition: all 0.3s ease;
            box-shadow: 0 2px 8px rgba(0, 0, 0, 0.05);
            
            &:hover {
                transform: translateY(-1px);
                box-shadow: 0 4px 12px rgba(0, 0, 0, 0.1);
            }
            
            &.edit {
                background: rgba(0, 122, 255, 0.7);
                color: white;
                
                &:hover {
                    background: rgba(0, 122, 255, 0.9);
                }
            }
            
            &.delete {
                background: rgba(255, 59, 48, 0.7);
                color: white;
                
                &:hover {
                    background: rgba(255, 59, 48, 0.9);
                }
            }
        }
        
        .form-checkbox {
            display: flex;
            align-items: center;
            gap: 0.5rem;
            color: rgba(255, 255, 255, 0.8);
            
            input {
                width: auto;
            }
        }
        
        .form-actions {
            display: flex;
            gap: 1rem;
            justify-content: flex-end;
            margin-top: 2rem;
        }

        .form-button {
            padding: 0.75rem 1.5rem;
            font-size: 0.9rem;
            
            &.secondary {
                background: rgba(255, 255, 255, 0.1);
                color: rgba(255, 255, 255, 0.8);
                border-color: rgba(255, 255, 255, 0.2);
            }
        }
        
        @media (max-width: 768px) {
            .stats-grid {
                grid-template-columns: repeat(2, 1fr);
            }

            .radio-grid {
                grid-template-columns: 1fr;
            }

            .modal-content {
                margin: 1rem;
                width: calc(100% - 2rem);
            }
        }
    `
];