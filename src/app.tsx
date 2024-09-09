import { createSignal } from "solid-js";
import "~/index.css";
import { Button } from "~/components/ui/button";

export default function App() {
    const [count, setCount] = createSignal(0);

    return (
        <main>
            <Button variant="ghost" onClick={() =>  setCount(count() + 1)}>
                {count()}
            </Button>
        </main>
    );
}
