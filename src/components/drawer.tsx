import {ReactNode, useState} from "react";
import {Collapsible, CollapsibleContent, CollapsibleTrigger} from "@/components/ui/collapsible.tsx";
import {CaretLeft, CaretRight} from "@phosphor-icons/react";

const Drawer = ({initialOpen = false, children}: { initialOpen: boolean, children: ReactNode }) => {
    const [isOpen, setOpen] = useState(initialOpen);
    const Icon = () => isOpen ? <CaretLeft/> : <CaretRight/>
    return (
        <Collapsible open={isOpen}>
            <div className="flex flex-row h-full bg-zinc-100">
                <CollapsibleContent>
                    {children}
                </CollapsibleContent>
                <CollapsibleTrigger className="bg-zinc-300 hover:bg-zinc-50 h-full" onClick={() => setOpen(!isOpen)}>
                    <Icon/>
                </CollapsibleTrigger>
            </div>
        </Collapsible>
    );
}

export {Drawer};