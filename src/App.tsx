import { House } from "phosphor-solid";
import NavigationButton from "./components/NavigationButton";

function App() {

  return (
    <div class="flex flex-row h-full bg-gray-100 dark:bg-slate-900">
      {/* sidebar */}
      <div class="bg-gray-100 dark:bg-slate-950">
        <NavigationButton>
          <House weight="duotone" size={24} />
        </NavigationButton>
      </div>
      {/* titlebar */}
      <div class="flex-1 flex flex-col h-full">
        <div class="p-4 flex flex-row items-center space-x-2">
          <p class="text-sm dark:text-white">
            <span class="font-bold">Polymer</span>
            <span>ium</span>
          </p>
        </div>
        <div class="flex-1"></div>
      </div>
    </div>
  );
}

export default App;
