import { Box, Flex, VStack } from "../styled-system/jsx";
import { Diamond, Home, Info, Package } from "lucide-solid";
import { A, Router, type RouteSectionProps } from "@solidjs/router";
import { Button } from "~/components/ui/button";
import { FileRoutes } from "@solidjs/start/router";
import type { JSXElement } from "solid-js";

interface NavItem {
    title: string | null;
    path: string;
    icon: JSXElement;
}

function ofButton(item: NavItem){
    return (
        <A href={item.path}>
            <Button width={48} variant={"ghost"}>
                {item.icon}
                {item.title}
            </Button>
        </A>
    );
}

export default function Layout(props: RouteSectionProps) {
    const main: NavItem[] = [
        {
            title: "Home",
            path: "/home",
            icon: <Home />,
        },
        {
            title: "Instances",
            path: "/instance",
            icon: <Package/>
        }
    ];

    return (
        <main>
            <Flex height="full">
                <Box flexShrink={0} width={"auto"}>
                    <VStack gap={"4"} margin={"4"}>
                        {main.map(ofButton)}
                    </VStack>
                </Box>
                <Box flex={"1"} background={"yellow"}>{props.children}</Box>
            </Flex>
        </main>
    );
}
