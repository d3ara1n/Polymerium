import { Box, Flex, VStack } from "../styled-system/jsx";
import { A, Router, type RouteSectionProps } from "@solidjs/router";
import { Card } from "./components/ui/card";
import { Button } from "./components/ui/button";
import { Text } from "./components/ui/text";
import { AppWindow, Minus, Square, X } from "lucide-solid";
import { IconButton } from "./components/ui/icon-button";

export default function Layout(props: RouteSectionProps) {
    

    return (
        <main>
            <Flex height="full">
                <Box flex={1}>{props.children}</Box>
                <Box flexShrink={"initial"} width={"auto"} padding={2}>
                    <Flex
                        background={"bg.emphasized"}
                        borderRadius={"md"}
                        height={"full"}
                        width={52}
                        shadow={"sm"}
                        data-tauri-drag-region
                    >
                        <Flex direction={"row"} width={"full"}>
                            <Box padding={2} flex={"1"}>
                                <Text size="xs" userSelect={"none"}>
                                    Polymerium
                                </Text>
                            </Box>
                            <Box>
                                <IconButton
                                    variant={"ghost"}
                                    size={"sm"}
                                    onClick={() => window.minmize}
                                >
                                    <Minus />
                                </IconButton>
                                <IconButton variant={"ghost"} size={"sm"}>
                                    <Square />
                                </IconButton>
                                <IconButton variant={"ghost"} size={"sm"}>
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
