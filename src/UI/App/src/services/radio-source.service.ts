import type {RadioSource} from "../interfaces/common.interfaces.ts";
import {createContext} from "@lit/context";

export const API_BASE_URL = import.meta.env.VITE_API_BASE_URL;

export class RadioSourceService {
    public async loadRadioSources(): Promise<RadioSource[]> {
        const response = await fetch(`${API_BASE_URL}/radio-sources`);
        if (!response.ok) {
            throw new Error(`HTTP error! status: ${response.status}`);
        }
        return await response.json();
    }

    public async updateRadioSource(sourceId: string, sourceUrl: string, isActive: boolean): Promise<void> {
        const response = await fetch(`${API_BASE_URL}/radio-sources/${sourceId}`, {
            method: 'PUT',
            headers: {
                'Content-Type': 'application/json',
            },
            body: JSON.stringify({
                newSourceUrl: sourceUrl,
                isActive: isActive,
            }),
        });
        if (!response.ok) {
            throw new Error(`HTTP error! status: ${response.status}`);
        }
    }

    public async deleteRadioSource(sourceId: string): Promise<void> {
        const response = await fetch(`${API_BASE_URL}/radio-sources/${sourceId}`, {
            method: 'DELETE',
        });
        if (!response.ok) {
            throw new Error(`HTTP error! status: ${response.status}`);
        }
    }

    public async addRadioSource(name: string, sourceUrl: string): Promise<RadioSource> {
        const response = await fetch(`${API_BASE_URL}/radio-sources/add`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
            },
            body: JSON.stringify(
                {
                    name: name,
                    sourceUrl: sourceUrl,
                }
            ),
        });
        if (response.ok) {
            return await response.json();
        }
        const errorText = await response.json();
        throw new Error(JSON.stringify(errorText));
    }
}

export const radioSourceServiceCtx = createContext<RadioSourceService>('radio-source-service');