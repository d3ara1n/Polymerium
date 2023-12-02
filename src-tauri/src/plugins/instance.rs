use libtrident::{instance::Instance, profile::{Entry, Profile}};
use tauri::{
    plugin::{Builder, TauriPlugin},
    Runtime, State, Manager,
};

use crate::manager::Manager as AppManager;

#[derive(Default)]
pub struct InstanceContainer {
    pub instance: Option<Instance>,
}

pub fn query_entries(manager: State<AppManager>) -> Vec<Entry> {
    manager.entries().iter().map(|s| s.to_owned()).collect()
}

pub fn load_instance(key: String, container: State<InstanceContainer>) -> Option<Profile> {
    todo!()
}

pub fn get_instance(container: State<InstanceContainer>) -> Option<Profile> {
    container.instance.as_ref().map(|instance| instance.profile().to_owned())
}

pub fn init<R: Runtime>() -> TauriPlugin<R> {
    Builder::new("instance")
        .invoke_handler(tauri::generate_handler![])
        .setup(|handle,_| {
            handle.manage(InstanceContainer::default());
            Ok(())
        })
        .build()
}
