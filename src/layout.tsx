import {Box, Flex, HStack} from "../styled-system/jsx";
import type {RouteSectionProps} from "@solidjs/router";
import {Text} from "./components/ui/text";
import {AlignJustify, LogOut, Minus, Settings, Square, X} from "lucide-solid";
import {IconButton} from "./components/ui/icon-button";
import {getCurrentWindow} from "@tauri-apps/api/window";
import {createSignal} from "solid-js";
import {Input} from "./components/ui/input";
import {Menu} from "./components/ui/menu";
import {Separator} from "./components/ui/styled/menu";

export default function Layout(props: RouteSectionProps) {
    const appWindow = getCurrentWindow();

    const [maximized, setMaximized] = createSignal(false);

    return (
        <main>
            <Flex height="full">
                <Box flex={1}>{props.children}</Box>
                <Box
                    flexShrink={"initial"}
                    width={"auto"}
                    padding={maximized() ? 0 : 2}
                >
                    <Flex
                        background={"bg.emphasized/75"}
                        borderRadius={maximized() ? "none" : "md"}
                        height={"full"}
                        width={"30vw"}
                        maxWidth={"96"}
                        minWidth={"64"}
                        shadow={"sm"}
                        direction={"column"}
                    >
                        <Flex direction={"row"} width={"full"}>
                            <Box flex={"1"} padding={2}>
                                <Text
                                    size="sm"
                                    userSelect={"none"}
                                    data-tauri-drag-region
                                >
                                    Polymerium
                                </Text>
                            </Box>
                            <Box>
                                <Menu.Root {...props}>
                                    <Menu.Trigger>
                                        <IconButton
                                            variant={"ghost"}
                                            size={"sm"}
                                        >
                                            <AlignJustify />
                                        </IconButton>
                                    </Menu.Trigger>
                                    <Menu.Positioner>
                                        <Menu.Content>
                                            <Menu.ItemGroup>
                                                <Menu.ItemGroupLabel>
                                                    Application
                                                </Menu.ItemGroupLabel>
                                                <Separator />
                                                <Menu.Item value="settings">
                                                    <HStack>
                                                        <Settings />
                                                        Settings
                                                    </HStack>
                                                </Menu.Item>
                                                <Separator />
                                                <Menu.Item value="logout">
                                                    <HStack gap="2">
                                                        <LogOut />
                                                        Logout
                                                    </HStack>
                                                </Menu.Item>
                                            </Menu.ItemGroup>
                                        </Menu.Content>
                                    </Menu.Positioner>
                                </Menu.Root>
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
                                        await appWindow.toggleMaximize();
                                        const maxed =
                                            await appWindow.isMaximized();
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
                        <Flex
                            flex={"1"}
                            data-tauri-drag-region
                            paddingLeft={4}
                            paddingRight={4}
                            paddingTop={1}
                            paddingBottom={2}
                        >
                            <Input size={"sm"} placeholder="Search instances" />
                        </Flex>
                    </Flex>
                </Box>
            </Flex>
        </main>
    );
}
