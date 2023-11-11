// Prevents additional console window on Windows in release, DO NOT REMOVE!!
#![cfg_attr(not(debug_assertions), windows_subsystem = "windows")]

use tauri::Manager;

fn main() {
    tauri::Builder::default()
        .setup(|app| {
            // let window = app.get_window("main").unwrap();
            // #[cfg(target_os = "windows")]
            // window_vibrancy::apply_mica(&window, None)
            //     .expect("Unsupported platform! 'apply_mica' is only supported on Windows");
            // 怎么把我的 TitleBar 给亚克力了
            Ok(())
        })
        .plugin(tauri_plugin_shell::init())
        .plugin(tauri_plugin_window::init())
        .run(tauri::generate_context!())
        .expect("error while running tauri application");
}
