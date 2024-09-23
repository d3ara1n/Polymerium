import {Router} from "@solidjs/router";
import "~/index.css";
import Layout from "~/layout";
import {FileRoutes} from "@solidjs/start/router";

export default function App() {
    return (
        <Router root={Layout}>
            <FileRoutes />
        </Router>
    );
}
