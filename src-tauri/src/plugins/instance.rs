use std::str::FromStr;
use std::sync::Mutex;
use packageurl::PackageUrl;
use tauri::{
    plugin::{Builder, TauriPlugin},
    Runtime, State,
};

use libtrident::profile::LOADERS;
use crate::manager::GameManager;
use crate::models::profile::Summary;

#[tauri::command]
pub fn scan(manager: State<Mutex<GameManager>>) -> Vec<Summary> {
    let m = manager.lock().unwrap();
    use crate::models::profile::Summary;
    m.scan().into_iter().map(|e| Summary {
        key: e.key,
        name: e.profile.name,
        author: e.profile.author,
        summary: e.profile.summary,
        thumbnail: e.profile.thumbnail.map(|u| u.as_str().to_owned()),
        version: e.profile.metadata.version,
        mod_loaders: e.profile.metadata.loaders.into_iter().filter_map(|l| LOADERS.iter().find(|&&i| i == l.id)).map(|&i| i.to_owned()).collect(),
        label: e.profile.reference.map(|s| {
            if let Ok(purl) = PackageUrl::from_str(s.as_ref()) {
                Some(purl.ty().to_owned())
            } else {
                None
            }
        }).flatten(),
        is_liked: false,
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
