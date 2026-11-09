# Foundation Source Standards

1. **One logical class, interface or enum per file**, named after that type. Never add a second top-level declaration to an existing source file.
2. **One method, one responsibility.** Split parsing, validation, state changes, IO and presentation into named operations instead of mixing them into a single callback.
3. Keep **public contracts** separate from implementations. All public Core contracts live under `Core/Contracts`; metadata under `Core/Models`; implementations under their domain.
4. **No new monolithic manager.** A bootstrap or composition root may construct and coordinate services, but domain behaviors belong in separate classes.
5. **Preserve existing namespaces and class identities** during migration. Moving a file does not require renaming its class or namespace.
6. **Nested Harmony patches** may need a `partial` parent class to preserve access to private methods. The organizer places each nested patch in a separate file with the same partial parent identity; the wrapper is not a second independent runtime class.
7. All new registrations must be **owner-scoped**, must reject duplicate IDs, and must return a disposable token for managed lifetimes.
8. **Native permanent registrations** cannot be cleaned up just because a module stopped. Document process-lifetime restrictions.
9. **Do not invent game behavior.** Core provides infrastructure; native gameplay changes route through the game's real classes and existing Foundation game service interfaces.
10. **Fail safely**: avoid changing other mods' Harmony patches, do not suppress native exceptions, and isolate third-party callbacks.
11. Keep interface revisions backward-compatible where possible; add new interfaces or extension methods for optional capabilities.
12. Build via the original project and the existing `INSTALL-FOUNDATION.cmd`. Core does not create an alternative loader, release pipeline, or duplicate toolchain.


## Supporting files

- Keep `INSTALL-FOUNDATION.cmd` as the only project-root command; it invokes `Tools/Install/Install-Foundation.ps1`.
- Repeatable build and packaging tools live in `Tools/Build/`; deployment in `Tools/Install/`; developer-only optional diagnostics in `Tools/Development/`.
- Current docs belong in `Docs/`, historical notes in `Docs/Archive/`; the SDK reference belongs in `SDK/`.
- Documentation illustrations belong in `Docs/Images/`; application sprites and assets belong in `Assets/Images/` or appropriate domain subfolders. The uploaded source had **no PNG files**.
- Do not retain once-only organizers, temporary installer hotfixes or post-install audit scripts in the release repository.
- Generated outputs are kept in `Releases/`, logs in `Verification/`, cached downloads in `.foundation-deps/`, and backups outside this source archive.
