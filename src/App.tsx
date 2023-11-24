import { Link, Navigate, Route, Routes, useLocation } from "react-router-dom";
import {
    Gear,
    House,
    Icon,
    IconContext,
    IconWeight,
    Info,
    Minus,
    Package,
    Plus,
    PlusCircle,
    Square,
    Storefront,
    X
} from "@phosphor-icons/react";
import { Button } from "@/components/ui/button.tsx";
import { getCurrent } from "@tauri-apps/api/window";
import React from "react";
import InstanceView from "@/views/instance.tsx";
import { ToggleGroup, ToggleGroupItem } from "@/components/ui/toggle-group.tsx";
import ConstructionView from "@/views/construction.tsx";
import WorkshopView from "./views/workshop";
import MarketView from "./views/market";
import { Separator } from "./components/ui/separator";
import { Toggle } from "./components/ui/toggle";

interface NavItem {
    path: string,
    icon: Icon,
    title: string | null,
    style: IconWeight
}

function App() {
    const window = getCurrent();
    const upRoutes: NavItem[] = [
        {
            path: "/workshop",
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
            path: "/market",
            icon: Storefront,
            title: "资源",
            style: "duotone"
        },
    ];
    const downRoutes: NavItem[] = [
        {
            path: "/settings",
            icon: Gear,
            title: null,
            style: "regular"
        },
        {
            path: "/about",
            icon: Info,
            title: null,
            style: "regular"
        }
    ];
    const location = useLocation();

    function ofButton(item: NavItem) {
        let Icon = item.icon;
        return (
            <Toggle asChild value={item.path} key={item.path}
                className={item.title === null ? "h-10 m-1 p-3  dark:text-zinc-50" : "h-16 m-1 p-3 dark:text-zinc-50"}
                pressed={location.pathname.startsWith(item.path)}
                disabled={location.pathname.startsWith(item.path)}>
                <Link to={item.path}>
                    <div className="flex flex-col items-center">
                        <Icon weight={location.pathname.startsWith(item.path) ? "fill" : item.style} />
                        <p className="text-primary">{item.title}</p>
                    </div>
                </Link>
            </Toggle>
        );
    }

    const upButtons = upRoutes.map(ofButton);
    const downButtons = downRoutes.map(ofButton);
    return (
        <div className="grid grid-cols-[4rem_auto] grid-rows-[2rem_auto] h-full bg-zinc-200 dark:bg-zinc-950">
            <div id="sidebar" className="col-start-1 row-start-2 flex flex-col">
                    <div className="flex flex-col h-full items-center">
                        <IconContext.Provider
                            value={{
                                size: 24,
                                color: "currentColor"
                            }}>
                            <div className="flex-1 flex flex-col items-center w-full"
                                data-tauri-drag-region={true}>
                                {upButtons}
                            </div>
                        </IconContext.Provider>
                        <IconContext.Provider
                            value={{
                                size: 24,
                                color: "currentColor"
                            }}>
                            <div className="flex flex-col items-center m-1"
                                data-tauri-drag-region={true}>
                                <Separator className="w-[90%]" />
                                {downButtons}
                            </div>
                        </IconContext.Provider>
                    </div>
            </div>
            <div id="titlebar" className="col-start-1 row-start-1 col-span-2">
                <div className="flex items-center h-full justify-between  overflow-hidden" data-tauri-drag-region={true}>
                    <p className="m-[0_0.75rem] text-md select-none align text-left dark:text-white">
                        <span className="font-bold" data-tauri-drag-region={true}>
                            Polymer
                        </span>
                        <span className="font-light" data-tauri-drag-region={true}>
                            ium
                        </span>
                    </p>
                    <div className="m-[0_0] flex dark:text-zinc-400">
                        <Button className="p-2" variant="ghost" size="icon" onClick={() => window.minimize()}>
                            <Minus />
                        </Button>
                        <Button className="p-2" variant="ghost" size="icon" onClick={() => window.toggleMaximize()}>
                            <Square />
                        </Button>
                        <Button className="p-2 hover:bg-red-400 dark:hover:bg-red-400" variant="ghost" size="icon" onClick={() => window.close()}>
                            <X />
                        </Button>
                    </div>
                </div>
            </div>
            <div id="page" className="col-start-2 row-start-2 flex flex-col h-full">
                <div className="flex-1 dark:text-white h-full">
                    <Routes>
                        <Route path="/" element={<Navigate to="/workshop" />} />
                        <Route path="/workshop" element={<WorkshopView />} />
                        <Route path="/instances" element={<InstanceView></InstanceView>}>
                            <Route path="/instances/:id" element={<InstanceView />} />
                        </Route>
                        <Route path="/market" element={<MarketView />} />
                        <Route path="/settings" element={<ConstructionView />} />
                    </Routes>
                </div>
            </div>

        </div>
    );
}

export default App;
