// Prevents additional console window on Windows in release, DO NOT REMOVE!!
#![cfg_attr(not(debug_assertions), windows_subsystem = "windows")]

mod manager;
mod plugins;
mod models;

use std::sync::Mutex;
use manager::GameManagerBuilder;

fn main() -> anyhow::Result<()> {
    let builder = GameManagerBuilder::new();
    let manager = builder.build()?;

    tauri::Builder::default()
        .manage(Mutex::new(manager))
        .plugin(tauri_plugin_window::init())
        .plugin(tauri_plugin_shell::init())
        .plugin(tauri_plugin_store::Builder::default().build())
        .plugin(plugins::instance::init())
        .plugin(plugins::market::init())
        .run(tauri::generate_context!())
        .map_err(|e| e.into())
}
