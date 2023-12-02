import { Drawer } from "@/components/drawer.tsx";
import { Separator } from "@/components/ui/separator.tsx";
import { invoke } from "@tauri-apps/api/primitives"
import {Button} from "@/components/ui/button.tsx";

export default function InstanceView() {


    invoke("instance/query_entries", {});
    
    // 这里仅展示 entries, 前端 drawer 会绕圈加载等待刷新，选择一个 entry 之后才会去从后端取 instance，离开页面之后会调用 drop(instance)。
    // manager 会维护这一状态，取 instance 意味着 manager.load 并放入 manager 的槽位，load 其他时 drop 当前的以实现保存。
    // 也就是前端没有状态维护能力，因为数据在后端。
    // 那其实可以设计成 mvu

    return (
        <div className="flex flex-col h-full">
            <Separator />
            <div className="flex-1 flex flex-row h-full">
                <Drawer initialOpen={true}>
                    <div className="w-[25vw]">
                        <div className="flex flex-row h-full">
                            <Button className="w-full" variant="secondary">
                                Overview
                            </Button>
                        </div>
                    </div>
                </Drawer>
                <div className="flex-1">
                    <p>Page Content</p>
                </div>
            </div>
        </div>
    );
}