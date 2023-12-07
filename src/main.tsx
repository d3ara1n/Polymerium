import React from "react";
import ReactDOM from "react-dom/client";
import "./styles.css";
import {BrowserRouter, createBrowserRouter, RouterProvider} from "react-router-dom";
import {routes} from "@/router.tsx";

ReactDOM.createRoot(document.getElementById("root") as HTMLElement).render(
    <React.StrictMode>
        <RouterProvider router={createBrowserRouter(routes)}/>
    </React.StrictMode>,
);
