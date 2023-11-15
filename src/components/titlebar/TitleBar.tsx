import {Button} from "@/components/ui/button.tsx";
import {Minus, Square, X} from "@phosphor-icons/react";

export default function TitleBar() {
    return (
        <div className="flex items-center h-full">
            <div className="flex-1">
                <p className="m-4 text-md">
                    <span className="font-bold">
                        Polymer
                    </span>
                    <span className="font-light">
                        ium
                    </span>
                </p>
            </div>
            <div className="flex items-center">
                <div className="flex-1">
                </div>
                <div className="m-4 flex">
                    <Button className="p-2" variant="ghost">
                        <Minus/>
                    </Button>
                    <Button className="p-2" variant="ghost">
                        <Square/>
                    </Button>
                    <Button className="p-2" variant="ghost">
                        <X/>
                    </Button>
                </div>
            </div>
        </div>
    );
}