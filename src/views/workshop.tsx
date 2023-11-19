import { Button } from "@/components/ui/button";
import { FileDashed, Storefront } from "@phosphor-icons/react";
import { Link } from "react-router-dom";

export default function WorkshopView() {
    return (
        <div className="grid grid-cols-2 grid-rows-2 gap-2 h-full p-[0rem_0.5rem_0.5rem_0]">
            <div className="relative rounded dark:bg-zinc-900 overflow-hidden">
                <div className="absolute -bottom-[30%] -right-[25%] w-full h-full dark:text-zinc-950">
                    <FileDashed size="100%" />
                </div>
                <div className="absolute flex flex-col items-center justify-center h-full w-full">
                    <div className="flex flex-col">
                        <p>
                            从
                            <span className="font-bold text-xl">空模板</span>
                        </p>
                        <p>
                            创建
                            <span className="font-bold text-lg">特定版本</span>
                            的新实例
                        </p>
                        <Button className="w-min self-center" variant="outline">
                            打开创建向导
                        </Button>
                    </div>
                </div>
            </div>
            <div className="relative rounded dark:bg-zinc-900 overflow-hidden">
                <div className="absolute -bottom-[30%] -right-[30%] w-full h-full dark:text-zinc-950">
                    <Storefront size="100%" />
                </div>
                <div className="absolute flex flex-col items-center justify-center h-full w-full">
                    <div className="flex flex-col">
                        <p>
                            访问
                            <span className="font-bold text-lg">资源中心</span>

                        </p>
                        <p>
                            检索并下载
                            <span className="font-bold text-xl">整合包</span>
                        </p>
                        <Button asChild className="self-end p-[1rem_0_1rem_0]" variant="link">
                            <Link to="/market">
                                转到资源中心→
                            </Link>
                        </Button>
                    </div>
                </div>
            </div>
            <div className="col-span-2 rounded dark:bg-zinc-900 flex flex-col items-center justify-center">
                <p>Import function not available</p>
            </div>
        </div>
    );
}