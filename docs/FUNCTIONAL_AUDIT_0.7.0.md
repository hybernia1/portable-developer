# Functional audit for 0.7.0

Historical audit retained for traceability. The requested terminal work was completed before 0.8.0.

The restricted terminal originally supported runtime commands and navigation but lacked common safe filesystem operations. The approved command registry added `pwd`, `ls`/`dir`, `cd`, `mkdir`, `touch`, `type`, `clear`, `help`, and explicit service lifecycle commands. Each command uses application services, normalizes paths under the active project, rejects links/reparse escapes and shell operators, and has deterministic help text.

The registry is independent of the WPF console so a future headless interface can reuse the same parser, validation, and execution services. Tests cover quoted names, traversal rejection, project-root protection, link escapes, existing destinations, localization-independent command names, and help output.
