// Prevents additional console window on Windows in release, DO NOT REMOVE!!
#![cfg_attr(not(debug_assertions), windows_subsystem = "windows")]

use std::sync::OnceLock;

use libtrident::machine::PersistMachine;

static MACHINE: OnceLock<PersistMachine> = OnceLock::new();

fn main() {
    #[cfg(debug_assertions)]
        let root = std::env::current_dir().unwrap().join(".trident");
    #[cfg(not(debug_assertions))]
        let root = Path::new("~/.trident");
    MACHINE.set(PersistMachine::new(root));

    tauri::Builder::default()
        .plugin(tauri_plugin_window::init())
        .plugin(tauri_plugin_shell::init())
        .run(tauri::generate_context!())
        .expect("error while running tauri application");
}
