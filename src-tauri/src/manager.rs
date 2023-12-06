use libtrident::{machine::InstantMachine, profile::Entry};

// 包含 instant machine，
// 保存 deploy 和 run 的 engine，并驱动进度，根据进度发布事件给前端

pub struct GameManager {
    machine: InstantMachine,
}

impl GameManager {
    pub fn builder() -> GameManagerBuilder {
        GameManagerBuilder::new()
    }

    pub fn scan(&self) -> Vec<Entry> {
        self.machine.scan()
    }
}

pub struct GameManagerBuilder {
    machine: InstantMachine,
}

impl GameManagerBuilder {
    pub fn new() -> Self {
        #[cfg(debug_assertions)]
        let root = std::env::current_dir().unwrap().join(".trident");
        #[cfg(not(debug_assertions))]
        let root = Path::new("~/.trident");
        Self {
            machine: InstantMachine::new(root),
        }
    }

    pub fn build(self) -> anyhow::Result<GameManager> {
        let machine = self.machine;
        let entries = machine.scan();
        Ok(GameManager {
            machine,
        })
    }
}
