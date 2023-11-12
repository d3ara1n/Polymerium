/* @refresh reload */
import { render } from "solid-js/web";

import "./styles.css";
import App from "./App";
import { Router } from "@solidjs/router";
import("preline");

render(() => (
    <Router>
        <App />
    </Router>
)
    , document.getElementById("root") as HTMLElement);
