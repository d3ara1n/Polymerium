import {RouteObject} from "react-router-dom";
import Layout from "@/layout.tsx";
import ConstructionView from "@/views/construction.tsx";
import NotFoundView from "@/views/not-found.tsx";

const routes: RouteObject[] = [
    {
        path: "/",
        element: <Layout/>,
        errorElement: <NotFoundView/>,
        children: [
            {
                path: "/home",
                element: <ConstructionView/>
            },
            {
                path: "/instances",
                lazy: () => import("@/views/instances.tsx"),
                children: [
                    
                ]
            }
        ]
    }
];

export {routes};