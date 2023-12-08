use tauri::plugin::{Builder, TauriPlugin};
use tauri::{Manager, Runtime, State};
use libtrident::repo::{LABEL_CURSEFORGE, LABEL_MODRINTH, Repository, RepositoryContext, RepositoryLabel};
use libtrident::repo::curseforge::CurseForge;
use libtrident::resources::ResourceKind;

#[tauri::command]
pub fn query_labels() -> Vec<String> {
    [LABEL_CURSEFORGE, LABEL_MODRINTH].into_iter().map(|s| s.to_owned()).collect()
}

pub fn search(label: &str, keyword: &str, page: usize, limit: usize, context: State<RepositoryContext>) -> Vec<String> {
    if let Ok(cast) = RepositoryLabel::try_from(label) {
        match cast {
            RepositoryLabel::CurseForge => CurseForge::search(keyword, page, limit, context.inner()),
            _ => todo!()
        };
        vec![]
    } else {
        vec![]
    }
}

pub fn init<R: Runtime>() -> TauriPlugin<R> {
    Builder::new("market")
        .invoke_handler(tauri::generate_handler![query_labels])
        .setup(|handle, _| {
            handle.manage(RepositoryContext {
                client: reqwest::blocking::Client::new(),
                game_version: None,
                kind: Some(ResourceKind::ModPack),
                mod_loader: None,
            });
            Ok(())
        })
        .build()
}
