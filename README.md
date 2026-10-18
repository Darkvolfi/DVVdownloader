<p align="center">
  <img src="src/windows/DVVdownloader/Assets/logo.png" width="128" alt="DVVdownloader logo" />
</p>

<h1 align="center">DVVdownloader</h1>

<p align="center">
  A free, open-source video and audio downloader for <b>Windows</b> and <b>Android</b>, powered by
  <a href="https://github.com/yt-dlp/yt-dlp">yt-dlp</a> and <a href="https://ffmpeg.org">FFmpeg</a>.
</p>

---

## Features

- Paste a link, pick a quality, download — video as MP4 (H.264 + AAC) or audio as MP3
- Separate video and audio streams are merged automatically, so high resolutions keep their sound
- Shows every available quality with its estimated file size
- Download queue with progress, speed, cancel and retry
- Download history
- **Android:** share a link from any app (YouTube, Pinterest, browser…) straight into DVVdownloader
- **Android:** the embedded yt-dlp updates itself on launch, so site changes are picked up without a new app release
- No accounts, no ads, no telemetry

## Supported sites

| Site | Windows | Android |
|---|---|---|
| YouTube (videos, Shorts, `youtu.be` links) | ✅ | ✅ |
| Pinterest (`pinterest.*`, `pin.it` links) | ✅ | ✅ |
| Direct media links (`.mp4`, `.mp3`, …) | ✅ | ✅ |
| Other sites supported by yt-dlp (Dailymotion, Twitch clips, SoundCloud, Bilibili, …) | ✅ | — |

Some sites (for example Vimeo, Reddit and Facebook) currently require a logged-in account and are not supported.

## Download

Get the latest build from the [Releases](../../releases) page.

### Windows

1. Download and extract the Windows archive.
2. Keep the `tools` folder next to `DVVdownloader.exe` (it contains `yt-dlp.exe`, `ffmpeg.exe` and `ffprobe.exe`).
3. Run `DVVdownloader.exe`. No installation or .NET runtime is needed.

### Android (8.0 or newer)

- `DVVdownloader.apk` — for almost all phones (arm64)
- `DVVdownloader-armv7.apk` — for older 32-bit phones

Because the app is not distributed through Google Play, Play Protect may show a warning when installing.
Tap **More details → Install anyway**. Downloaded files can be opened from the **Completed** tab.

## Building from source

### Windows

Requirements: Windows 10/11, [.NET 10 SDK](https://dotnet.microsoft.com/download).

1. Put `yt-dlp.exe`, `ffmpeg.exe` and `ffprobe.exe` into `src/windows/DVVdownloader/tools/`
   (see [tools/README.md](src/windows/DVVdownloader/tools/README.md)).
2. Build:

   ```bash
   dotnet publish src/windows/DVVdownloader/DVVdownloader.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o dist/windows
   ```

3. Run the tests:

   ```bash
   dotnet test tests/DVVdownloader.Tests
   ```

### Android

Requirements: JDK 21, Android SDK (platform 35, build-tools 35), Gradle 8.14.
Android Studio provides all of these.

```bash
cd src/android
gradle assembleDebug
```

APKs are written to `src/android/app/build/outputs/apk/`. The build produces one APK per CPU architecture plus a universal APK.

**Release signing (optional):** `gradle assembleRelease` signs the APKs if it finds a
`keystore.properties` file at `~/.android-keys/dvvdownloader/keystore.properties`
(or at the path in the `DVV_KEYSTORE_PROPS` environment variable):

```properties
storeFile=/path/to/your-release-key.p12
storePassword=...
keyAlias=...
keyPassword=...
```

Without it, the release APKs are built unsigned.

## Project structure

```
src/
├── windows/DVVdownloader/   WPF app (.NET 10, MVVM) – runs yt-dlp/FFmpeg as external processes
└── android/                 Kotlin app – embeds yt-dlp, FFmpeg and QuickJS via youtubedl-android
tests/DVVdownloader.Tests/   xUnit tests for the Windows app
```

## Acknowledgements

- [yt-dlp](https://github.com/yt-dlp/yt-dlp) — the download engine on both platforms
- [FFmpeg](https://ffmpeg.org) — merging and audio conversion
- [youtubedl-android](https://github.com/yausername/youtubedl-android) — yt-dlp, Python, FFmpeg and QuickJS for Android
- [OkHttp](https://square.github.io/okhttp/) and the AndroidX / Material Components libraries

## Disclaimer

DVVdownloader is intended for downloading content you have the right to download — your own uploads,
content under a permissive license, or content the owner allows to be saved. Respect the terms of service
of the sites you use and the copyright of the creators.

## License

DVVdownloader is free software licensed under the [GNU General Public License v3.0](LICENSE).
