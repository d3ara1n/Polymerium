import { Form, FormControl, FormField } from "@/components/ui/form";
import { Input } from "@/components/ui/input";
import { ScrollArea } from "@/components/ui/scroll-area";
import { useForm } from "react-hook-form";

export default function MarketView() {
    const form = useForm();
    return (

        <div className="h-[calc(100vh-2rem)]">
            <ScrollArea className="h-full shrink-0">
                <div className="relative flex flex-col">
                    <div className="flex-1 order-last h-full">
                    </div>
                    <div className="sticky top-0 h-20 backdrop-blur">
                    </div>
                </div>
            </ScrollArea>
        </div>
    );
}