# Windows Defender, SmartScreen, and application reputation

Portable Developer does not ask users to disable Microsoft Defender, SmartScreen, or Smart App Control. A warning must first be classified before it is reported or investigated.

## Different kinds of blocks

- Microsoft Defender Antivirus reports a detection name and records a threat event, commonly event 1116/1117.
- Microsoft Defender SmartScreen evaluates the reputation of a downloaded URL, file hash, and publisher.
- Smart App Control may block an unsigned executable even when Defender Antivirus has not classified it as malware.

An unsigned release starts without publisher reputation, and every new unsigned file hash starts again without inherited reputation. A self-signed certificate does not solve this. The durable project plan is a consistent public signature linked to the verified build, subject to the [Code signing policy](CODE_SIGNING_POLICY.md).

## What a useful report contains

1. exact Portable Developer version and download URL;
2. whether the block happened during download, extraction, or launch;
3. exact product and wording: Defender Antivirus, SmartScreen, or Smart App Control;
4. detection name, if one is displayed;
5. security intelligence version and Windows version;
6. SHA-256 of the blocked file;
7. screenshot with private paths or account information removed.

If Microsoft classifies an official file as malware or unsafe rather than merely unknown, submit the exact release file through the [Microsoft Security Intelligence software developer portal](https://www.microsoft.com/en-us/wdsi/filesubmission) as an incorrect detection. Link the public repository, tag, release, workflow run, checksum, and this policy. Never submit private projects, databases, logs, or user data with the sample.

## Release 0.6.0 verification

- release: <https://github.com/hybernia1/portable-developer/releases/tag/v0.6.0>;
- release ZIP SHA-256: `f1c577001dd1c86128dd20f9203e817562f0fc7b7046eaff5465e19dfb15c5fd`;
- official `PortableDeveloper.exe` SHA-256: `eee3ab344c72a9532a5f10df27f2ca93a6e9b9fc848dbd44a2b3d625241e598e`;
- build workflow: <https://github.com/hybernia1/portable-developer/actions/runs/32580394163>.

On the maintainer machine, the Defender operational log contained no Portable Developer malware detection event and no active Defender threat after the reported block. The official executable was unsigned. This evidence supports a reputation block diagnosis but does not replace the exact message or Microsoft's analysis.
