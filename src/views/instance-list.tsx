import {
    Button,
    Card,
    CardFooter,
    CardHeader, Divider, Image,
    Popover,
    PopoverContent,
    PopoverTrigger, Tooltip
} from "@nextui-org/react";
import {ArrowLineDown, Funnel, Plus, Star} from "@phosphor-icons/react";
import {invoke} from "@tauri-apps/api/primitives";

export default function InstanceListView() {
    invoke("plugin:instance|scan",{}).then(list => {
        console.log(list)
    });
    return (
        <div className="p-[0_1rem_1rem_1rem]">
            <div className="flex flex-row justify-between">
                <div></div>
                <div className="flex flex-row space-x-2">
                    <Button color="primary" size="sm" startContent={<Plus/>}>
                        创建
                    </Button>
                    <Button size="sm" startContent={<ArrowLineDown/>}>
                        导入
                    </Button>
                    <Popover placement="bottom">
                        <PopoverTrigger>
                            <Button size="sm" isIconOnly={true}>
                                <Funnel size={16}/>
                            </Button>
                        </PopoverTrigger>
                        <PopoverContent>
                            <p>没有过滤选项</p>
                        </PopoverContent>
                    </Popover>
                </div>
            </div>
            <Tooltip placement="bottom" content="All The Mods 9">
                <Card isPressable={true} isHoverable={true} isFooterBlurred={true} className="w-[12rem]">
                    <CardHeader className="p-0">
                        <Image className="drop-shadow" isZoomed={true}
                               src="https://media.forgecdn.net/avatars/902/338/638350403793040080.png"/>
                    </CardHeader>
                    <CardFooter className="absolute t-0 z-10 rounded-none flex-row-reverse">
                        <div>
                            <Button isIconOnly={true} variant="light">
                                {/* 收藏可以通过 tauri 的持久化 store 中存 key, 最终得到一个包含 isLiked 的模型。是否收藏不应该位于 Trident 数据中。 */}
                                <Star size={24}/>
                            </Button>
                        </div>
                        <div className="flex-1 flex flex-col items-start p-0">
                            <p className="text-white/70 font-bold text-tiny uppercase">CurseForge</p>
                            <h4> All The Mods 9 </h4>
                        </div>
                    </CardFooter>
                </Card>
            </Tooltip>
        </div>
    );
}