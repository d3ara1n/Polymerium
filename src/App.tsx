import {Routes, Route, Navigate, Link, useLocation} from "react-router-dom";
import {
    Gear,
    House,
    Icon,
    IconContext,
    IconWeight,
    Info,
    Minus,
    Package,
    Square,
    Storefront,
    X
} from "@phosphor-icons/react";
import {Button} from "@/components/ui/button.tsx";
import {getCurrent} from "@tauri-apps/api/window";
import HomeView from "@/views/HomeView.tsx";

interface NavItem {
    path: string,
    icon: Icon,
    title: string | null
    style: IconWeight
}

function App() {
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
            path: "/resources",
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
            <Button asChild key={item.path} className={item.title === null ? "h-10 m-1 p-3" : "h-16 m-1 p-3"}
                    variant="ghost"
                    disabled={location.pathname.startsWith(item.path)}>
                <Link to={item.path}>
                    <div className="flex flex-col items-center">
                        <Icon weight={location.pathname.startsWith(item.path) ? "fill" : item.style}/>
                        <p className="text-primary">{item.title}</p>
                    </div>
                </Link>
            </Button>
        );
    }

    const upButtons = upRoutes.map(ofButton);
    const downButtons = downRoutes.map(ofButton);
    return (
        <div className="grid grid-cols-[4rem_auto] h-full">
            <div className="flex flex-col bg-zinc-200 dark:bg-zinc-800" data-tauri-drag-region={true}>
                <img className="m-3" alt="logo" src="/logo.png" data-tauri-drag-region={true}/>
                <div className="flex-1 flex flex-col h-full items-center">
                    <IconContext.Provider
                        value={{
                            size: 24,
                            color: "currentColor"
                        }}>
                        <div className="flex-1 flex flex-col items-center" data-tauri-drag-region={true}>
                            {upButtons}
                        </div>
                    </IconContext.Provider>
                    <IconContext.Provider
                        value={{
                            size: 24,
                            color: "currentColor"
                        }}>
                        <div className="flex flex-col items-center m-1" data-tauri-drag-region={true}>
                            {downButtons}
                        </div>
                    </IconContext.Provider>
                </div>
            </div>
            <div className="flex flex-col bg-zinc-300 dark:bg-zinc-900">
                <div className="h-16">
                    <div className="flex items-center h-full justify-between" data-tauri-drag-region={true}>
                        <p className="m-4 text-md select-none align text-left">
                            <span className="font-bold" data-tauri-drag-region={true}>
                                Polymer
                            </span>
                            <span className="font-light" data-tauri-drag-region={true}>
                                ium
                            </span>
                        </p>
                        <div className="m-4 flex">
                            <Button className="p-2" variant="ghost" onClick={() => window.minimize()}>
                                <Minus/>
                            </Button>
                            <Button className="p-2" variant="ghost" onClick={() => window.toggleMaximize()}>
                                <Square/>
                            </Button>
                            <Button className="p-2" variant="ghost" onClick={() => window.close()}>
                                <X/>
                            </Button>
                        </div>
                    </div>
                </div>
                <div className="flex-1">
                    <Routes>
                        <Route path="/" element={<Navigate to="/home"/>}/>
                        <Route path="/home" element={<HomeView/>}/>
                        <Route path="/instances" element={<p>This is Instances</p>}/>
                        <Route path="/resources" element={<p>This is Resources</p>}/>
                        <Route path="/settings" element={<p>This is Settings</p>}/>
                        <Route path="/about" element={<p>This is About</p>}/>
                    </Routes>
                </div>
            </div>
        </div>
    );
}

export default App;
