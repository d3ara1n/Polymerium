import {Navigate, Route, Routes} from "@solidjs/router";
import {IconContext} from "phosphor-solid";

function App() {
    return (
        <div class="flex flex-row h-full">
            <IconContext.Provider
                value={{
                    size: 28,
                    weight: "duotone",
                }}>
                <div class="flex flex-col w-16 bg-zinc-300 justify-between">
                    <img class="m-3" src="/logo.png" alt="logo"/>
                    <div class="flex flex-1 flex-col items-center space-x-2">
                    </div>
                </div>
            </IconContext.Provider>
            <div class="flex-1 bg-zinc-200 drop-shadow">
                <Routes>
                    <Route path="/" element={<Navigate href="/home"/>}/>
                    <Route path="/home" element={
                        <div>
                            <p>Home</p>
                        </div>
                    }/>
                </Routes>
            </div>
        </div>
    );
}

export default App;
