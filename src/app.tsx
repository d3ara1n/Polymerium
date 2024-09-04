import {createSignal} from "solid-js";
import "~/index.css";
import {Button} from "~/components/ui/button";

export default function App() {
    const [count, setCount] = createSignal(0);

    return (
        <main>
            <h1>Hello world!</h1>
            <Button class="increment" onClick={() => setCount(count() + 1)} type="button">
                Clicks: {count()}
            </Button>
            <p>
                Visit{" "}
                <a href="https://start.solidjs.com" target="_blank">
                    start.solidjs.com
                </a>{" "}
                to learn how to build SolidStart apps.
            </p>
        </main>
    );
}
