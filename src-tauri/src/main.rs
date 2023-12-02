// Prevents additional console window on Windows in release, DO NOT REMOVE!!
#![cfg_attr(not(debug_assertions), windows_subsystem = "windows")]

mod manager;
mod plugins;

use manager::ManagerBuilder;

fn main() -> anyhow::Result<()> {
    let builder = ManagerBuilder::new();
    let manager = builder.build()?;

    tauri::Builder::default()
        .manage(manager)
        .plugin(tauri_plugin_window::init())
        .plugin(tauri_plugin_shell::init())
        .plugin(plugins::instance::init())
        .run(tauri::generate_context!())
        .map_err(|e| e.into())
}
