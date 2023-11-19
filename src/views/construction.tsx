import {Barricade} from "@phosphor-icons/react";

export default function ConstructionView() {
    return (
        <div className="flex flex-col h-full place-content-center items-center bg-zinc-100 dark:bg-zinc-800 rounded-[0.5rem_0_0_0]">
            <Barricade weight="duotone" size="38vw" className="text-zinc-950 dark:text-zinc-100"/>
            <p className="text-[4vw]">Page is under construction.</p>
        </div>
    );
}