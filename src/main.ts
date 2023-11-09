import { createApp } from "vue";
import "./styles.css";
import App from "./App.vue";
import router from "./router";

import("preline");

createApp(App).use(router).mount("#app");
