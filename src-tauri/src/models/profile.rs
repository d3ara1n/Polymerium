use serde::{Deserialize, Serialize};
use ts_rs::TS;

#[derive(TS, Serialize, Deserialize)]
#[ts(export, export_to = "../src/bindings/profile/summary.ts")]
pub struct Summary {
    pub key: String,
    pub is_liked: bool,
    pub name: String,
    pub author: String,
    pub summary: String,
    pub thumbnail: Option<String>,
    pub label: Option<String>,
    pub version: String,
    pub mod_loaders: Vec<String>,
}