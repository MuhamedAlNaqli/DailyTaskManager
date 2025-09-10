# Daily Task Manager - Versioning & Update Management Implementation Roadmap

**Status**: 🟡 Planning Complete - Ready for Implementation  
**Created**: September 10, 2025  
**Last Updated**: September 10, 2025

---

## 🎯 Overview

This roadmap tracks the implementation of professional versioning and automatic update management for Daily Task Manager, transitioning from manual versioning to a fully automated CI/CD-driven approach.

## 📋 Implementation Phases

### Phase 1: Foundation Setup (Core Versioning) 🔄 **READY TO START**

#### 1.1 Implement Semantic Versioning (SemVer)
- [ ] **Task**: Add GitVersion.MsBuild NuGet package to all projects
  - Target: `DailyTaskManager.Avalonia.csproj`, `DailyTaskManager.Core.csproj`, `DailyTaskManager.Infrastructure.csproj`
  - Package: `GitVersion.MsBuild` latest stable
  
- [ ] **Task**: Create GitVersion.yml configuration file
  - Location: Repository root
  - Configuration: Mainline mode with semantic versioning
  - Branch strategies for main/develop/feature branches
  
- [ ] **Task**: Update MSBuild properties to use GitVersion
  - Remove hardcoded versions from project files
  - Configure automatic version properties
  - Test version generation locally

- [ ] **Task**: Sync installer versioning with application version
  - Update `installer/installer.wxs` to use dynamic versioning
  - Modify `installer/build-installer.ps1` to pass version from GitVersion
  - Test MSI version consistency

**Estimated Time**: 2-3 hours  
**Priority**: High (Foundation for everything else)  
**Status**: Not Started

---

### Phase 2: Auto-Update System (NetSparkleUpdater) ⏳ **PENDING PHASE 1**

#### 2.1 Integrate NetSparkleUpdater.UI.Avalonia
- [ ] **Task**: Add NetSparkleUpdater NuGet packages
  - Package: `NetSparkleUpdater.UI.Avalonia` v3.x
  - Dependencies: Core NetSparkle packages
  
- [ ] **Task**: Implement update service in Avalonia app
  - Create UpdateService class
  - Add update checking logic
  - Integrate with app startup and menu
  
- [ ] **Task**: Design update UI integration
  - Update available notifications
  - Release notes display
  - Download progress indicators
  
- [ ] **Task**: Configure appcast generation
  - XML appcast format
  - Hosting strategy (GitHub Releases vs custom)
  - Security considerations (signatures)

#### 2.2 Update Infrastructure Setup
- [ ] **Task**: Setup update hosting
  - Choose: GitHub Releases (recommended) or custom server
  - Configure appcast endpoint
  - Test update delivery mechanism
  
- [ ] **Task**: Implement delta updates (optional)
  - Binary delta generation
  - Bandwidth optimization
  - Fallback to full updates

**Estimated Time**: 4-5 hours  
**Priority**: High  
**Status**: Blocked by Phase 1

---

### Phase 3: CI/CD Automation 🔒 **PENDING PHASE 1-2**

#### 3.1 GitHub Actions Workflow
- [ ] **Task**: Create build and release workflow
  - File: `.github/workflows/release.yml`
  - Triggers: Tag creation, manual dispatch
  - Build matrix: Windows x64
  
- [ ] **Task**: Implement version-driven releases
  - GitVersion integration in CI
  - Conditional release creation
  - Asset uploading to GitHub Releases
  
- [ ] **Task**: Setup automated testing in CI
  - Unit tests
  - Integration tests
  - UI automation tests (existing framework)

#### 3.2 Automated Release Process
- [ ] **Task**: Configure conventional commits
  - Commit message standards
  - Automatic version bumping rules
  - Documentation for team
  
- [ ] **Task**: Auto-generate release notes
  - Parse commit messages
  - Generate changelog
  - Include in GitHub Releases

**Estimated Time**: 3-4 hours  
**Priority**: Medium  
**Status**: Blocked by Phase 1-2

---

### Phase 4: Enhanced Installer Versioning 🔒 **PENDING PHASE 1**

#### 4.1 WiX Major Upgrade Strategy
- [ ] **Task**: Enhance WiX installer for proper upgrades
  - Review current `installer.wxs` MajorUpgrade configuration
  - Test upgrade scenarios
  - Handle migration/settings preservation
  
- [ ] **Task**: Implement version constraints
  - Windows Installer version limits
  - Validate version format compliance
  - Error handling for invalid versions

#### 4.2 Deployment Flexibility
- [ ] **Task**: Multiple release channels
  - Stable vs Beta vs Development
  - Channel-specific update URLs
  - User preference for update channel
  
- [ ] **Task**: Rollback capability
  - Version history tracking
  - Downgrade prevention/warnings
  - Emergency rollback procedures

**Estimated Time**: 2-3 hours  
**Priority**: Low  
**Status**: Blocked by Phase 1

---

## 📊 Progress Tracking

### Current Status Summary
- **Overall Progress**: 0% (Research Complete)
- **Phase 1**: 🔴 Not Started (Ready to begin)
- **Phase 2**: ⏸️ Blocked
- **Phase 3**: ⏸️ Blocked  
- **Phase 4**: ⏸️ Blocked

### Next Actions
1. **Immediate**: Start Phase 1.1 - Add GitVersion to projects
2. **This Week**: Complete Phase 1 foundation
3. **Next**: Begin Phase 2 auto-update integration

### Dependencies
- **External**: GitVersion.MsBuild, NetSparkleUpdater.UI.Avalonia packages
- **Internal**: Current working installer system (✅ Complete)
- **Infrastructure**: GitHub repository access, Actions enabled

---

## 🔧 Technical Decisions Made

### Chosen Technologies
- **Versioning**: GitVersion (SemVer automation)
- **Updates**: NetSparkleUpdater.UI.Avalonia (native Avalonia support)
- **CI/CD**: GitHub Actions (integrated with repository)
- **Distribution**: GitHub Releases (free, reliable)
- **Installer**: WiX v4 (current working solution)

### Alternatives Considered
- **WiX# vs XML**: Staying with XML (working, simpler)
- **Squirrel vs NetSparkle**: NetSparkle for Avalonia native UI
- **Custom server vs GitHub**: GitHub for simplicity and cost

---

## 📝 Notes & Considerations

### Implementation Notes
- Start with Phase 1 as it's foundational and low-risk
- Test thoroughly in development before affecting production builds
- Consider creating a separate test repository for CI/CD experimentation
- Document any breaking changes for team members

### Risk Mitigation
- Keep current manual versioning as backup during transition
- Test installer upgrades on clean Windows VMs
- Validate update mechanism with beta testers before production
- Maintain rollback procedures for each phase

### Future Enhancements
- Code signing for enhanced security
- Crash reporting integration
- Usage analytics (privacy-compliant)
- Multi-platform update support (macOS, Linux)

---

## 🔗 Useful References

### Documentation Links
- [GitVersion Documentation](https://gitversion.net/docs/)
- [NetSparkleUpdater GitHub](https://github.com/NetSparkleUpdater/NetSparkle)
- [WiX Toolset v4 Docs](https://wixtoolset.org/docs/v4/)
- [GitHub Actions Documentation](https://docs.github.com/en/actions)

### Key Files to Monitor
- `GitVersion.yml` (to be created)
- `installer/installer.wxs` (version integration)
- `installer/build-installer.ps1` (version passing)
- `.github/workflows/release.yml` (to be created)

**Last Review**: September 10, 2025  
**Next Review**: After Phase 1 completion