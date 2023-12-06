use std::sync::Mutex;
use libtrident::{profile::{Entry, Profile}};
use tauri::{
    plugin::{Builder, TauriPlugin},
    Runtime, State, Manager,
};

use serde::{Deserialize, Serialize};
use crate::manager::GameManager;

#[derive(Serialize, Deserialize)]
pub struct WrappedProfile {
    pub key: String,
    pub profile: Profile,
}

#[tauri::command]
pub fn scan(manager: State<Mutex<GameManager>>) -> Vec<WrappedProfile> {
    let m = manager.lock().unwrap();
    m.scan().into_iter().map(|e| WrappedProfile {
        key: e.key,
        profile: e.profile,
    }).collect()
}

pub fn init<R: Runtime>() -> TauriPlugin<R> {
    Builder::new("instance")
        .invoke_handler(tauri::generate_handler![scan])
        .setup(|handle, _| {
            Ok(())
        })
        .build()
}
