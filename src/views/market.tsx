import {
    Input,
    Select,
    SelectItem
} from "@nextui-org/react";
import {useState} from "react";
import {invoke} from "@tauri-apps/api/primitives";
import {useLoaderData} from "react-router-dom";

export async function loader({}) {
    return await invoke("plugin:market|query_labels", {});
}

export function Component() {

    const [keyword, setKeyword] = useState<string>("");

    const labels = useLoaderData() as string[];

    return (
        <div>
            <div
                className="sticky top-0 h-[8rem] z-[99] backdrop-blur flex flex-row space-x-3 items-center p-4 bg-gradient-to-br from-blue-500/50 via-pink-300/50 to-yellow-200/50"
                data-tauri-drag-region={true}>
                <Input className="flex-1" label="关键词" isClearable={true} value={keyword}
                       onClear={() => setKeyword("")} onChange={(event) => setKeyword(event.target.value)}
                       classNames={{
                           label: "text-black/50 dark:text-white/90",
                           input: [
                               "bg-transparent",
                               "text-black/90 dark:text-white/90",
                               "placeholder:text-default-700/50 dark:placeholder:text-white/60",
                           ],
                           innerWrapper: "bg-transparent",
                           inputWrapper: [
                               "shadow-xl",
                               "bg-default-200/50",
                               "dark:bg-default/60",
                               "backdrop-blur-xl",
                               "backdrop-saturate-200",
                           ],
                       }}/>
                <Select className="w-[10rem]" label="仓库" items={labels.map(label => {
                    return {
                        key: label,
                        title: label.toUpperCase()
                    };
                })} defaultSelectedKeys={[labels[0]]}
                        classNames={{
                            label: "text-black/50 dark:text-white/90",
                            trigger: [
                                "shadow-xl",
                                "bg-default-200/50",
                                "dark:bg-default/60",
                                "backdrop-blur-xl",
                                "backdrop-saturate-200",
                            ],
                        }}>
                    {
                        label => <SelectItem key={label.key} title={label.title}/>
                    }
                </Select>
            </div>

            <div className="p-[0_1rem_1rem_1rem]">
            </div>
        </div>
    );
}

// <Card className="w-[12rem]">
//     <CardHeader>
//         <h2 className="text-xl">All The Mods 9</h2>
//     </CardHeader>
//     <CardBody className="p-4">
//         <Image isBlurred={true}
//                src="https://media.forgecdn.net/avatars/902/338/638350403793040080.png"/>
//     </CardBody>
//     <CardFooter className="flex flex-col items-stretch">
//         <div className="py-1 flex flex-col">
//             <h4 className="text-foreground/60 text-tiny flex flex-row items-center space-x-1">
//                             <span>
//                                 <User/>
//                             </span>
//                 <span>ATM Team</span></h4>
//
//             <div className="flex flex-row justify-between">
//                 <p className="text-foreground/60 text-tiny flex flex-row items-center space-x-1">
//                             <span>
//                                 <Clock/>
//                             </span>
//                     <span>2023.2.4</span>
//                 </p>
//                 <p className="text-foreground/60 text-tiny flex flex-row items-center space-x-1">
//                             <span>
//                                 <ArrowLineDown/>
//                             </span>
//                     <span>
//                                 2.3M
//                             </span>
//                 </p>
//             </div>
//         </div>
//         <div className="flex flex-row space-x-2">
//             <Button fullWidth={true} size="sm">
//                 查看
//             </Button>
//             <Button color="primary" fullWidth={true} size="sm">
//                 添加
//             </Button>
//         </div>
//     </CardFooter>
// </Card>
// <Card className="flex flex-row h-[10rem] w-[20rem]">
//     <CardBody className="p-4">
//         <Image className="w-[8rem]" isBlurred={true}
//                src="https://media.forgecdn.net/avatars/884/231/638318279994673553.png"/>
//     </CardBody>
//     <CardHeader className="w-[10]">
//         <div>
//             <h2>Desertopolis</h2>
//         </div>
//     </CardHeader>
// </Card>