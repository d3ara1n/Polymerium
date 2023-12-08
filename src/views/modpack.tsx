import {Card, CardBody, CardHeader, Tab, Tabs} from "@nextui-org/react";
import {useLoaderData} from "react-router-dom";

export async function loader({}) {
    return null;
}

export function Component() {
    const data = useLoaderData();
    return (
        <div className="flex flex-row">
            <div className="flex-1">
                <Tabs variant="underlined">
                    <Tab key="gallery" title="Gallery">
                        
                    </Tab>
                    <Tab key="description" title="Description"></Tab>
                </Tabs>
            </div>
            <div className="w-[40%]">
                <Card>
                    <CardHeader>
                        <h4>Versions</h4>
                    </CardHeader>
                    <CardBody></CardBody>
                </Card>
            </div>
        </div>
    );
}