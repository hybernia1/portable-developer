# Source layout

- `PortableDeveloper.Domain` contains models and domain rules.
- `PortableDeveloper.Application` contains interfaces, use cases, and controllers.
- `PortableDeveloper.Infrastructure` contains filesystem, process, package, persistence, and runtime implementations.
- `PortableDeveloper.App` contains the WPF shell, localization, styles, and interaction orchestration.

Dependencies point inward: App and Infrastructure depend on Application/Domain contracts. The UI must not bypass those contracts to control processes or portable state directly.
