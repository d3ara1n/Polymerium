import {FileX} from "@phosphor-icons/react";

export default function NotFoundView(){
    return (
        <div className="flex flex-col h-full place-content-center items-center">
            <FileX weight="duotone" size="38vw" className="text-zinc-950 dark:text-zinc-100"/>
            <p className="text-[4vw]">Fatal Errorrrrrrrrrrrrr</p>
        </div>
    );
}