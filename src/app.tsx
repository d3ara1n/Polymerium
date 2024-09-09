import { Router } from "@solidjs/router";
import { FileRoutes } from "node_modules/@solidjs/start/dist/router";
import { css } from "styled-system/css";
import { Box } from "styled-system/jsx";
import "~/index.css";

export default function App() {

    return (
        <main>
            <Box class={css({
                background: "black"
            })}>

            </Box>
            <Router url="about">
                <FileRoutes />
            </Router>
        </main>
    );
}
