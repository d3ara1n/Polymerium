import {Link} from "react-router-dom";
import {Icon, IconContext} from "@phosphor-icons/react";
import {Button} from "@/components/ui/button.tsx";
import {useState} from "react";
import {Separator} from "@/components/ui/separator.tsx";

export type NavItem = {
    path: string,
    icon: Icon,
    title: string
}

export default function NavBar({items}: { items: NavItem[] }) {
    const [selected, setSelected] = useState("");
    const buttons = items.map((item, _) => {
        let Icon = item.icon;
        return (
            <Button asChild className="h-16 m-1 p-3" variant="ghost" disabled={selected === item.path}
                    onClick={() => setSelected(item.path)}>
                <Link to={item.path}>
                    <div>
                        <Icon weight={selected == item.path ? "fill" : "duotone"}
                              color={selected == item.path ? "currentColor" : "currentColor"}/>
                        <p className="text-primary">{item.title}</p>
                    </div>
                </Link>
            </Button>
        )
    });
    return (
        <IconContext.Provider
            value={{
                size: 28,
            }}>
            <div className="flex-1 flex flex-col h-full items-center">
                <div className="flex-1 flex flex-col items-center">
                    {buttons}
                </div>
            </div>
        </IconContext.Provider>
    );
}