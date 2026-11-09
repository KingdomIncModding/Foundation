# Foundation 0.11.0 — Consolidated Stage B Source and Migration Release

**Delivery model:** one complete Foundation source ZIP. No changes-only or incremental releases. `Foundation.dll` is the **only production DLL**. The independent `Foundation.StageBVerification.dll` is development-only and removable.

**Baseline:** `Foundation-0.10.7-Stage-B1-Core-InterMod-Complete-Source.zip` based on the latest user-supplied `Foundation-Source-20261109-125339-96756.zip` and the accepted Gate A roadmap. Native A1–A7 implementations are preserved.

## Contents

- B1: prior module/lifecycle/registry/service/event/inter-mod work preserved, with no parallel registry.
- B2: new owner-aware asset views and safe owner release, explicit asset dependencies with cycle protection; native content owner lookups; optional safe schema migrations in existing `FoundationModData` save format.
- B3: exact source/property discovery, opt-in link forwarding, missing-provider recovery, owner-scoped cleanup, serialization of connection IDs into the mod's existing save callbacks. No native pipes are manipulated.
- B4: context-owned native-themed panel registration and existing ModConfig/BepInEx Options integration, with original native controls and UI behavior retained.
- B5: developer console/inspector uses public Foundation World/Flow/IO and research-recovery readers, existing diagnostics retained.
- B6: all five items classified **REWRITE** by owner decision; R05 owner-aware patch rollback for first-class patch session failures; R14 native game UI creation delegated through `FoundationApi.NativeUiFactory`; R19 unified one-shot independent smoke tests with SHA and PASS/FAIL/PENDING; R23 real-native research lookup through Foundation API while unknown research preservation logic stays byte-format compatible; R24 native tech/icon prerequisites through public Foundation API while workspace node registration and factory behavior remain intact.
- Old public interfaces `IFoundationAsset`, `IFoundationAssets`, `IGameContentRegistry`, `IModSaveRegistry`, `ISignalRegistry`, `IModBarContext`, `ICorePatchManager`, `IWorkspaceNodeRegistry` remain unchanged. New B2/B3 discovery/migration capabilities use *derived* interfaces and additional public `FoundationApi` properties so external interface implementers need not change.
- Documentation: 24-item source coverage matrix, B6 per-item before/after migration and compatibility guide, Stage B acceptance checklist and 24-workstream smoke coverage manifest.

## Verification provenance

- Local/source: 68 Stage B static contract checks PASS; 24 B1 source checks PASS; A1 21 PASS/1 SKIP, A2 27 PASS/8 SKIP, A3–A7 93 PASS/1 SKIP; UI source 39 PASS/1 SKIP; game theme 24 PASS; screenshot source and smoke isolation PASS; source structure PASS.
- **Windows/.NET compilation not performed in this environment**. Unity real-time tests and external mod consumer compatibility not performed on this source revision. User-reported B1 and A1–A7 passing test suites pertain to **earlier builds**, not proof that 0.11.0 will load.
- Independent Stage B smoke reports explicit **PENDING** for live save/restore, native content registration, UI visuals, missing-tech recovery, workspace-node persistence, multi-mod Harmony conflict tests and other operations not safely testable via a one-shot startup check. These must not be represented as PASS.

**Do not mark Stage B 100% complete** until the exact installed DLL has successfully compiled and passed the runtime acceptance checklist, including backwards compatibility of real external mods. The purpose of this package is to deliver the integrated source and test coverage without removing established functionality.
