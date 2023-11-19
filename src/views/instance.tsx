import {Drawer} from "@/components/drawer.tsx";
import {Separator} from "@/components/ui/separator.tsx";

export default function InstanceView() {
    return (
        <div className="flex flex-col h-full">
            <Separator/>
            <div className="flex-1 flex flex-row h-full">
                <Drawer initialOpen={true}>
                    <div className="w-[25vw]">
                    </div>
                </Drawer>
                <div className="flex-1">
                    <p>Page Content</p>
                </div>
            </div>
        </div>
    );
}