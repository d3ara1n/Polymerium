import {Box, Flex, VStack} from "../styled-system/jsx";
import {A, Router, type RouteSectionProps} from "@solidjs/router";
import {Card} from "./components/ui/card";
import {Button} from "./components/ui/button";
import {Text} from "./components/ui/text";
import {AppWindow, Minus, Square, X} from "lucide-solid";
import {IconButton} from "./components/ui/icon-button";
import {getCurrentWindow} from "@tauri-apps/api/window";
import {createSignal} from "solid-js";

export default async function Layout(props: RouteSectionProps) {
    const appWindow = getCurrentWindow();

    const [maximized, setMaximized] = createSignal(false);

    return (
        <main>
            <Flex height="full">
                <Box flex={1}>{props.children}</Box>
                <Box flexShrink={"initial"} width={"auto"} padding={2}>
                    <Flex
                        background={"bg.emphasized"}
                        borderRadius={"md"}
                        height={"full"}
                        width={"64"}
                        shadow={"sm"}
                        data-tauri-drag-region
                    >
                        <Flex direction={"row"} width={"full"}>
                            <Box padding={maximized() ? 0 : 2} flex={"1"}>
                                <Text size="xs" userSelect={"none"}>
                                    Polymerium
                                </Text>
                            </Box>
                            <Box>
                                <IconButton
                                    variant={"ghost"}
                                    size={"sm"}
                                    onClick={() => appWindow.minimize()}
                                >
                                    <Minus />
                                </IconButton>
                                <IconButton
                                    variant={"ghost"}
                                    size={"sm"}
                                    onclick={async () => {
                                        const maxed = await appWindow.isMaximized();
                                        console.log(maxed);
                                        await appWindow.toggleMaximize();
                                        setMaximized(maxed);
                                    }}
                                >
                                    <Square />
                                </IconButton>
                                <IconButton
                                    variant={"ghost"}
                                    size={"sm"}
                                    onClick={() => appWindow.close()}
                                >
                                    <X />
                                </IconButton>
                            </Box>
                        </Flex>
                    </Flex>
                </Box>
            </Flex>
        </main>
    );
}
