use libtrident::{instance::Instance, machine::InstantMachine};

// 包含 instant machine，
// 保存 deploy 和 run 的 engine，并驱动进度，根据进度发布事件给前端

pub struct Manager {
    machine: InstantMachine,
    instances: Vec<Instance>,
}

impl Manager {
    pub fn builder() -> ManagerBuilder {
        ManagerBuilder::new()
    }

    pub fn entries(&self) -> &[Instance] {
        &self.instances
    }
}

pub struct ManagerBuilder {
    machine: InstantMachine,
}

impl ManagerBuilder {
    pub fn new() -> Self {
        #[cfg(debug_assertions)]
        let root = std::env::current_dir().unwrap().join(".trident");
        #[cfg(not(debug_assertions))]
        let root = Path::new("~/.trident");
        Self {
            machine: InstantMachine::new(root),
        }
    }

    pub fn build(self) -> anyhow::Result<Manager> {
        let machine = self.machine;
        let instances = machine
            .entries()
            .iter()
            .filter_map(|e| machine.get_instance(e).ok())
            .collect();
        Ok(Manager { machine, instances })
    }
}
