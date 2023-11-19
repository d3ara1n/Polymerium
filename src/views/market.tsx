import { ScrollArea } from "@/components/ui/scroll-area";

export default function MarketView() {
    return (
        <div className="h-[calc(100vh-2rem)]">
            <ScrollArea className="h-full shrink-0">
                <div className="relative flex flex-col">
                    <div className="flex-1 order-last h-full">
                        <div className="bg-red-500 h-[60rem] w-full"></div>
                    </div>
                    <div className="sticky top-0 h-20 border-b border-b-zinc-700 backdrop-blur"></div>
                </div>
            </ScrollArea>
        </div>
    );
}