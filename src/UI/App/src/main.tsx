import {createRoot} from "react-dom/client";
import {StrictMode} from "react";
import './views/dashboard/dashboard-app';
import '../global.d.ts';

createRoot(document.getElementById('root')!).render(
    <StrictMode>
        <dashboard-app></dashboard-app>
    </StrictMode>
);