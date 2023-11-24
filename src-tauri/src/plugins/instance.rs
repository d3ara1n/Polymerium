use libtrident::{instance::Instance, Url};
use tauri::{
    plugin::{Builder, TauriPlugin},
    Runtime,
};

use crate::MANAGER;

pub struct InstanceSummary {
    pub id: String,
    pub name: String,
    pub author: String,
    pub summary: String,
    pub thumbnail: Option<Url>,
    pub reference: Option<String>,
}

impl InstanceSummary {
    pub fn from_instance(inst: &Instance) -> Self {
        let profile = inst.profile();
        Self {
            id: profile.id.clone(),
            name: profile.name.clone(),
            author: profile.author.clone(),
            summary: profile.summary.clone(),
            thumbnail: profile.thumbnail.clone(),
            reference: profile.reference.clone(),
        }
    }
}

pub fn query_instances() -> Vec<InstanceSummary> {
    let manager = MANAGER.get().unwrap();
    manager
        .entries()
        .iter()
        .map(|i| InstanceSummary::from_instance(i))
        .collect()
}

pub fn init<R: Runtime>() -> TauriPlugin<R> {
    Builder::new("instance")
        .invoke_handler(tauri::generate_handler![])
        .build()
}
