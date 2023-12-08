import {Navigate, RouteObject} from "react-router-dom";
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
                path: "/",
                element: <Navigate to="/home"/>
            },
            {
                path: "/home",
                element: <ConstructionView/>
            },
            {
                path: "/showroom",
                lazy: () => import("@/views/showroom.tsx"),
                children: []
            },
            {
                path: "/market",
                lazy: () => import("@/views/market.tsx"),
                children: []
            },
        ]
    }
];

export {routes};