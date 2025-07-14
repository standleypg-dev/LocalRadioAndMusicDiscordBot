import type {UserStatsDto} from "../interfaces/common.interfaces.ts";
import {createContext} from "@lit/context";
import {API_BASE_URL} from "./radio-source.service.ts";

export class UserService {
    public async loadUsers(): Promise<UserStatsDto[]> {
        const response = await fetch(`${API_BASE_URL}/users`, {
            method: 'GET',
            headers: {
                'Content-Type': 'application/json',
            }
        });
        if (!response.ok) {
            throw new Error(`HTTP error! status: ${response.status}`);
        }
        return await response.json();
    }
    
    public async loginUser(userName: string, password:string): Promise<{ token: string }> {
        const response = await fetch(`${API_BASE_URL}/login`, {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
            },
            body: JSON.stringify({userName, password})
        });
        if (!response.ok) {
            throw new Error(`HTTP error! status: ${response.status}`);
        }
        return await response.json();
    }
}

export const userServiceContext = createContext<UserService>('user-service');