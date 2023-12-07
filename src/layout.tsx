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
            path: "/instances",
            icon: Package,
            title: "实例",
            style: "duotone"
        },
        {
            path: "/workshop",
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
            <div className="flex flex-row h-full">
                <div id="sidebar" className="w-16 flex flex-col bg-white">
                    <div className="flex flex-col h-full items-center">
                        <IconContext.Provider
                            value={{
                                size: 24,
                                color: "currentColor"
                            }}>
                            <div className="flex-1 flex flex-col items-center w-full space-y-2 m-2"
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
                <div className="flex-1 h-full shrink-0 overflow-scroll bg-zinc-100">
                    <div className="relative flex flex-col">
                        <div className="flex-1 order-last h-[calc(100vh-4rem)]">
                            <div className="h-full w-full">
                                {/*<Routes>*/}
                                {/*    <Route path="/" element={<Navigate to="/home"/>}/>*/}
                                {/*    <Route path="/home" element={<ConstructionView/>}/>*/}
                                {/*    <Route path="/instances" lazy={() => import("@/views/instances.tsx")}>*/}
                                {/*        <Route path="/instances/:id" element={<></>}/>*/}
                                {/*    </Route>*/}
                                {/*    <Route path="/workshop" element={<WorkshopView/>}/>*/}
                                {/*    <Route path="/settings" element={<></>}/>*/}
                                {/*</Routes>*/}
                                <Outlet/>
                            </div>
                        </div>
                        <div
                            className="sticky top-0 h-[4rem] z-[999] backdrop-blur flex flex-row justify-between items-center p-4"
                            data-tauri-drag-region={true}>
                            <div>
                                <p className="flex flex-row items-center select-none">
                                    <span className="font-bold" data-tauri-drag-region={true}>Polymer</span>
                                    <span data-tauri-drag-region={true}>ium</span>
                                </p>
                            </div>
                            <div>
                            </div>
                            <div className="flex flex-row">
                                <Button variant="light" isIconOnly={true} onClick={() => window.minimize()}>
                                    <Minus/>
                                </Button>
                                <Button variant="light" isIconOnly={true} onClick={() => window.toggleMaximize()}>
                                    <Square/>
                                </Button>
                                <Button className="data-[hover=true]:bg-red-300" variant="light" isIconOnly={true}
                                        onClick={() => window.close()}>
                                    <X/>
                                </Button>
                            </div>
                        </div>
                    </div>
                </div>
            </div>
        </NextUIProvider>
    );
}

export default Layout;
