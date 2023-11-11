function TitleBar() {
    return (
        <div>
            <div class="p-1 flex items-center justify-between">
                {/* return button and title search bar */}
                <div class="flex flex-row m-2 space-x-2">
                    <img src="../Logo.png" width="20" height="16"/>
                    <p class="text-sm">
                        <span class="font-bold">Polymer</span>
                        <span>ium</span>
                    </p>
                </div>
                <div class="bg-yellow-50">
                    <input type="text" class="p-2 block w-64 border-none bg-gray-100 rounded-md text-xs focus:border-blue-500 focus:ring-blue-500 dark:bg-slate-900 dark:border-gray-700 dark:text-gray-400 dark:focus:ring-gray-600" placeholder="Search something..." />
                </div>
                <div class="bg-blue-50">
                    <p>Button</p>
                </div>
            </div>
        </div>
    );
}

export default TitleBar;