import {BrowserRouter as Router, Routes, Route} from "react-router-dom";
import NavBar, {NavItem} from "@/components/sidebar/NavBar.tsx";
import {Gear, House, Package, Storefront} from "@phosphor-icons/react";
import {Separator} from "@/components/ui/separator.tsx";
import TitleBar from "@/components/titlebar/TitleBar.tsx";

function App() {
    const routes: NavItem[] = [
        {
            path: "/home",
            icon: House,
            title: "主页"
        },
        {
            path: "/instances",
            icon: Package,
            title: "实例"
        },
        {
            path: "/resources",
            icon: Storefront,
            title: "资源"
        },
        {
            path: "/settings",
            icon: Gear,
            title: "设置"
        }
    ];
    return (
        <Router>
            <div className="grid grid-cols-[4rem_auto] h-full">
                <div className="flex flex-col bg-zinc-200">
                    <img className="m-3" alt="logo" src="/logo.png"/>
                    <NavBar items={routes}/>
                </div>
                <div className="flex flex-col bg-zinc-300">
                    <div className="h-16">
                        <TitleBar/>
                    </div>
                    <div className="flex-1">
                        <Routes>
                            <Route path="/home" element={<p>This is Home</p>}>
                            </Route>
                        </Routes>
                    </div>
                </div>
            </div>
        </Router>
    );
}

export default App;
