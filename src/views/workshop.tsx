import {Button, Card, CardBody, CardFooter, CardHeader, Image} from "@nextui-org/react";
import {ArrowLineDown, Clock, User} from "@phosphor-icons/react";

export default function WorkshopView() {
    return (
        <div className="p-[0_1rem_1rem_1rem]">
            <Card className="w-[12rem] relative">
                <CardHeader>
                    <h2 className="text-xl">All The Mods 9</h2>
                </CardHeader>
                <CardBody className="p-4">
                    <Image isBlurred={true}
                           src="https://media.forgecdn.net/avatars/902/338/638350403793040080.png"/>
                </CardBody>
                <CardFooter className="flex flex-col items-stretch">
                    <div className="py-1 flex flex-col">
                        <h4 className="text-foreground/60 text-tiny flex flex-row items-center space-x-1">
                            <span>
                                <User/>
                            </span>
                            <span>ATM Team</span></h4>

                        <div className="flex flex-row justify-between">
                            <p className="text-foreground/60 text-tiny flex flex-row items-center space-x-1">
                            <span>
                                <Clock/>
                            </span>
                                <span>2023.2.4</span>
                            </p>
                            <p className="text-foreground/60 text-tiny flex flex-row items-center space-x-1">
                            <span>
                                <ArrowLineDown/>
                            </span>
                                <span>
                                2.3M
                            </span>
                            </p>
                        </div>
                    </div>
                    <div className="flex flex-row space-x-2">
                        <Button fullWidth={true} size="sm">
                            查看
                        </Button>
                        <Button color="primary" fullWidth={true} size="sm">
                            添加
                        </Button>
                    </div>
                </CardFooter>
            </Card>
        </div>
    );
}