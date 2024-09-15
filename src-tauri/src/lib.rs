use specta_typescript::Typescript;
use tauri::Manager;
use tauri_plugin_os::{platform, version, Version};
use tauri_specta::{collect_commands, Builder};
use window_vibrancy::apply_acrylic;
#[allow(unused_imports)]
use window_vibrancy::{apply_mica, apply_vibrancy, NSVisualEffectMaterial};

#[tauri::command]
#[specta::specta]
fn hello(name: String) -> String {
    format!("Hello {}", name)
}

#[cfg_attr(mobile, tauri::mobile_entry_point)]
pub fn run() {
    let specta = Builder::<tauri::Wry>::new().commands(collect_commands![hello]);

    #[cfg(debug_assertions)] // <- Only export on non-release builds
    specta
        .export(Typescript::default(), "../src/bindings.ts")
        .expect("Failed to export typescript bindings");

    #[cfg(debug_assertions)]
    let builder = tauri::Builder::default().plugin(tauri_plugin_devtools::init());
    #[cfg(not(debug_assertions))]
    let builder = tauri::Builder::default();

    builder
        .setup(move |app| {
            let window = app.get_webview_window("main").unwrap();
            let platform = platform();

            match platform{
                "windows" => 
                    if let Version::Semantic(10, _, patch) = version(){
                        if patch > 22000{
                            apply_mica(&window, None)
                            .expect("Unsupported platform! 'apply_mica' is only supported on Windows 11");
                        }else{
                            apply_acrylic(&window, None)
                        .expect("Unsupported platform! 'apply_acrylic' is only supported on Windows 11");
                        }
                    },
                "macos" => {
                    apply_vibrancy(&window, NSVisualEffectMaterial::HudWindow, None, None)
                        .expect("Unsupported platform! 'apply_vibrancy' is only supported on macOS");
                }
                _ => panic!("Unsupported platform")
            };            

            specta.mount_events(app);

            Ok(())
        })
        .run(tauri::generate_context!())
        .expect("error while running tauri application");
}
