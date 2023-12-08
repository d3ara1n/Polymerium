import {Navigate, Outlet, Route, Routes, useLocation, useNavigate} from "react-router-dom";
import {
    Gear,
    House,
    Icon,
    IconContext,
    IconWeight,
    Info, Minus,
    Package, Square,
    Storefront, X,
} from "@phosphor-icons/react";
import React from "react";
import {Button, Divider, Link, NextUIProvider} from "@nextui-org/react";
import {getCurrent} from "@tauri-apps/api/window";
import WorkshopView from "@/views/workshop.tsx";
import ConstructionView from "@/views/construction.tsx";

interface NavItem {
    path: string,
    icon: Icon,
    title: string | null,
    style: IconWeight
}

function Layout() {
    const navigate = useNavigate();
    const window = getCurrent();
    const upRoutes: NavItem[] = [
        {
            path: "/home",
            icon: House,
            title: "主页",
            style: "duotone"
        },
        {
            path: "/showroom",
            icon: Package,
            title: "实例",
            style: "duotone"
        },
        {
            path: "/market",
            icon: Storefront,
            title: "资源",
            style: "duotone"
        },
    ];
    const downRoutes: NavItem[] = [
        {
            path: "/about",
            icon: Info,
            title: null,
            style: "regular"
        },
        {
            path: "/settings",
            icon: Gear,
            title: null,
            style: "regular"
        }
    ];
    const location = useLocation();

    function ofButton(item: NavItem) {
        let Icon = item.icon;
        return (
            <Button key={item.path}
                    className={(item.title === null ? "w-12 h-12" : "w-12 h-14") + " " + (location.pathname.startsWith(item.path) ? "bg-zinc-200" : "")}
                    size="sm" isIconOnly={true}
                    variant="light" as={Link} href={item.path}>
                <div className="flex flex-col items-center">
                    <Icon weight={location.pathname.startsWith(item.path) ? "fill" : item.style}/>
                    <p>{item.title}</p>
                </div>
            </Button>
        );
    }

    const upButtons = upRoutes.map(ofButton);
    const downButtons = downRoutes.map(ofButton);
    return (
        <NextUIProvider navigate={navigate} className="h-full">
            <div className="flex flex-row h-[100vh] w-[100vw]">
                <div id="sidebar" className="w-16 flex flex-col bg-white">
                    <div className="flex flex-col h-full items-center">
                        <IconContext.Provider
                            value={{
                                size: 24,
                                color: "currentColor"
                            }}>
                            <div className="flex-1 flex flex-col items-center w-full space-y-2 m-2 overflow-clip"
                                 data-tauri-drag-region={true}>
                                <img alt="logo" src="/logo.png" className="w-[3rem]" data-tauri-drag-region={true}/>
                                {upButtons}
                            </div>
                        </IconContext.Provider>
                        <IconContext.Provider
                            value={{
                                size: 24,
                                color: "currentColor"
                            }}>
                            <div className="flex flex-col items-center m-2 space-y-2"
                                 data-tauri-drag-region={true}>
                                <Divider className="w-[90%]"/>
                                {downButtons}
                            </div>
                        </IconContext.Provider>
                    </div>
                </div>
                <div className="flex-1 h-full bg-zinc-100 overflow-clip">
                    <div className="relative h-full flex flex-col">
                        <div className="flex-1 h-full">
                            <div className="h-full w-full overflow-auto">
                                <Outlet/>
                            </div>
                        </div>
                        <div className="absolute z-[100] top-0 right-0 h-8 flex flex-row-reverse overflow-clip">
                            <Button className="rounded-none h-8 w-8 data-[hover=true]:bg-red-300" variant="light"
                                    isIconOnly={true}
                                    onClick={() => window.close()}>
                                <X/>
                            </Button>
                            <Button className="rounded-none h-8 w-8" variant="light" isIconOnly={true}
                                    onClick={() => window.toggleMaximize()}>
                                <Square/>
                            </Button>
                            <Button className="rounded-none h-8 w-8" variant="light" isIconOnly={true}
                                    onClick={() => window.minimize()}>
                                <Minus/>
                            </Button>
                        </div>
                    </div>
                </div>
            </div>
        </NextUIProvider>
    );
}

export default Layout;
