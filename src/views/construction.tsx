import {Barricade} from "@phosphor-icons/react";

export default function ConstructionView() {
    return (
        <div className="flex flex-col h-full place-content-center items-center">
            <Barricade weight="duotone" size="38vw" className="text-zinc-950 dark:text-zinc-100"/>
            <p className="text-[4vw]">Page is under construction.</p>
        </div>
    );
}