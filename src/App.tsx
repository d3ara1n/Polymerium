import SideBar from "./components/SideBar";
import TitleBar from "./components/TitleBar";

import "./App.css";

function App() {

  return (
    <div class="flex flex-col h-full dark:bg-gray-800">
      <TitleBar />
      <div class="flex-1">
        <div class="flex flex-row h-full">
          <SideBar />
          <div class="flex-1 shadow-inner rounded-md bg-gray-50"></div>
        </div>
      </div>
    </div>
  );
}

export default App;
