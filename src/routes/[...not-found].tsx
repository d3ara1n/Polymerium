import {Home} from "lucide-solid";
import {Container} from "styled-system/jsx";
import {center} from "styled-system/patterns";

export default function NotFound() {
    return (
        <Container alignContent={"center"} height={"full"}>
            <Home size={64} />
        </Container>
    );
}
