# SlideShowScreenSaverWithVideo — Session Context

## Project

WPF screensaver that plays a random slide show of JPEG images and video files from a chosen folder tree, with cross-dissolve transitions. Built on top of `FFMEVideoPlayerControlLib` (the reusable player control from the sibling FFMEVideoPlayerControl project).

**Solution:** `D:\@Software\SlideShowScreenSaverWithVideo`  
**Project folder:** `SlideShowScreenSaver\`  
**Target:** net10.0-windows, x64, WPF + WindowsForms  
**Version:** 1.0.0.3  
**FFmpeg DLLs:** Copied from `..\..\FFMEVideoPlayerControl\FFMEVideoPlayerTester\ffmpegbin\` at build time — no local copy  
**Registry key:** `Software\Greenberg Consulting\SlideShowScreenSaver\1.0`

---

## Files

| File | Purpose |
|------|---------|
| `App.xaml.cs` | Startup: sets `FFmpegDirectory`, dispatches to screensaver / preview / config based on args |
| `MainWindow.xaml` | Full-screen window: two Image controls (dual-buffer), VideoPlayer, InputBlockingOverlay, DisplayText |
| `MainWindow.xaml.cs` | All slide show logic: media loading, transitions, keyboard/mouse, timers, video events |
| `Settings.cs` | Registry-backed settings (extends `UserRegistrySettings`) |
| `UserRegistrySettings.cs` | Base class: open/read/write registry helpers |
| `RegistryKeyExtensions.cs` | Extension methods: `GetString`, `GetInteger`, `GetBoolean`, `Write` on `RegistryKey` |
| `SettingsDialog.xaml/.cs` | `/c` config dialog: folder picker, timing slider, font size, video options |
| `Blackout.xaml/.cs` | Black window covering non-primary monitors |
| `TextPath.cs` | Custom outlined text `Shape` (gold fill, black stroke) for `DisplayText` |
| `DirectionEnum.cs` | `None / Previous / Next` enum for `Navigate()` |
| `InteropDefs.cs` | Win32 P/Invoke structs/enums for preview mode (`RECT`, `WindowStyles`, `Win32API`) |
| `AssemblyInfo.cs` | Marks output as not CLS-compliant |

---

## NuGet Dependencies

| Package | Version | Purpose |
|---------|---------|---------|
| `Microsoft-Windows10-APICodePack-Shell` | 1.1.8 | `CommonOpenFileDialog` for folder picker in settings |
| `WinCopies.WindowsAPICodePack` | 2.12.0 | Required by APICodePack-Shell |

**Project reference:** `..\..\FFMEVideoPlayerControl\FFMEVideoPlayerControlLib\FFMEVideoPlayerControlLib.csproj`

---

## Screensaver Startup Modes

`App.xaml.cs` dispatches on the first command-line argument:

| Arg | Mode | Behavior |
|-----|------|---------|
| (none) or `/s` | Screensaver | Primary screen → `MainWindow`; each other screen → `Blackout` |
| `/p <hwnd>` | Preview | `MainWindow` embedded into the preview pane via `HwndSource`; images only (no video) |
| `/c` | Config | Opens `SettingsDialog` modal |

`Library.FFmpegDirectory = AppDomain.CurrentDomain.BaseDirectory` must be set before any window opens.

---

## Settings (Registry)

All settings read/written on every property access (no in-memory cache beyond `Timing` which is read in `ReadSettingsFromRegistry`):

| Property | Default | Purpose |
|----------|---------|---------|
| `Timing` | 3 s | Seconds per image slide |
| `PhotoFolder` | `MyPictures` | Root folder scanned recursively |
| `ShowFileName` | true | Whether to show path text overlay |
| `DisplayByFileName` | true | Show filename only |
| `DisplayByFolderName` | true | Show parent folder name only |
| `DisplayByFolderFileName` | true | Show `folder\file` |
| `DisplayByPath` | true | Show full path |
| `DisplayFontSize` | 26 | Text overlay font size (pt) |
| `IncludeVideos` | true | Include video files in the slide pool |
| `ClipVideos` | true | Cut videos off after `ClipDuration` seconds |
| `ClipDuration` | 4 s | Max seconds to play a video clip before advancing |

---

## Media Loading

`LoadMediaFolder()` recursively enumerates `*.jpg` / `*.jpeg` and video extensions. Preview mode strips videos (a tiny preview panel makes video pointless).

**Video extensions:** `.mp4 .mkv .mov .avi .wmv .m4v .webm` (case-insensitive `HashSet`)

---

## Architecture — Key Decisions

### Dual image buffer
Two `Image` controls (`Image1`, `Image2`) share the same Grid slot. `CurrentCtrlIndex` alternates 0/1 on each advance. `ImageControls[CurrentCtrlIndex]` is `imgNew`; the previous index is `imgOld`.

### History list
`HistoryList` (max 50 entries) + `HistoryIndex` track the random sequence so Left/Right navigation can replay previous slides. Random picks extend the list forward; navigating back/forward within the list doesn't add new entries.

### Slide timer + pause auto-resume
- `TimerChangeSlide` (interval = `Settings.Timing`) fires `ShowSlide()`.
- `Stop()` stops `TimerChangeSlide` and starts `TimerResumeAfterPause` (2 s tick).
- `pauseCountDown` counts down from 60; at 0 `Start()` resumes.
- `DisplayText` shows a dotted "paused …" suffix during countdown.

---

## Transition Logic

### Image → Image
XAML `Storyboard` resources `FadeIn` / `FadeOut` (0.75 s `DoubleAnimation` on `Opacity`). `FadeOut` is cloned so concurrent animations don't share state.

### Image → Video (`ShowVideoSlide`)
1. `VideoPlayer` set to `Opacity=0`, `Visibility=Visible`; `InputBlockingOverlay` shown.
2. `VideoPlayer.Open(path)` starts (AutoPlay=true because `ControlsVisible=false`).
3. `MediaOpened` fires once the first frame is decoded → cross-dissolve begins (image fades out, player fades in).
4. On `fadeOut.Completed`: collapse both image controls if still in video mode.
5. Guard in `MediaOpened` handler: if `!_videoIsActive || VideoPlayer.Source != capturedPath`, skip (slide already moved on).

### Video → Image
1. `_videoIsActive = false`; set `imgNew` visible at opacity 0.
2. Cross-dissolve: `imgNew` fades in, `VideoPlayer` fades out (0.75 s).
3. On `fadeOut.Completed`: stop player, collapse `VideoPlayer` and overlay, restart `TimerChangeSlide`. Guard: if `_videoIsActive` (a new video started during the fade), leave player visible.

### Video → Video
`VideoPlayer.UseDualBuffer` is true by default. Uses `VideoPlayer.OpenDualBuffer(path)` which handles the seamless swap and cross-dissolve internally (`FadeEffectBetweenVideos=true`). Falls back to `VideoPlayer.Open()` if dual-buffer is disabled.

### Clip timer
`StartClipTimer()` creates a one-shot `DispatcherTimer` for `Settings.ClipDuration` seconds that calls `ShowSlide()`. Cancelled when any slide transition begins (`StopClipTimer()` is always called first in `ShowSlide()`). `VideoPlayer_MediaEnded` also cancels it (video ended before clip limit).

---

## Interactive Mode (video only)

Toggled by pressing **V** while a video is playing:

| Mode | `InputBlockingOverlay` | `VideoPlayer.ControlsVisible` | Key/Mouse behavior |
|------|------------------------|-------------------------------|-------------------|
| Screensaver (default) | Visible (blocks clicks) | false | Space=pause/resume slide, Left/Right=navigate, any other=exit |
| Interactive | Collapsed | true | Space/Left/Right control video; other keys ignored; mouse reaches player controls |

Escape always exits regardless of mode.

---

## Keyboard Shortcuts

| Key | Context | Action |
|-----|---------|--------|
| Space | Normal, video inactive | Pause / resume slide show |
| Space | Normal, video active | Pause / resume video |
| Left | Normal | Navigate to previous slide (pauses) |
| Right | Normal | Navigate to next slide (pauses) |
| V | Normal, video active | Toggle interactive mode |
| Space | Interactive | Pause / resume video |
| Left | Interactive | Step backward one frame |
| Right | Interactive | Step forward one frame |
| Escape | Any | Exit screensaver |
| Any other | Normal | Exit screensaver |
| Any other | Interactive | Ignored (do not exit) |

---

## Image Loading

`CreateImageSource()` opens the file with `BitmapDecoder` (`BitmapCacheOption.OnLoad` so the stream can be closed), then calls `ApplyExifOrientation()` to check EXIF tag `/app1/ifd/{ushort=274}` (orientation 274). Handles all 8 EXIF orientations via `TransformedBitmap` + `RotateTransform` / `ScaleTransform` chains. Result is `Freeze()`d.

---

## Display Text

`TextPath` (bottom-left, gold fill + black stroke) shows the current path formatted by `DisplayTextBasedOnSettings()`:

- `ShowFileName=false` → empty string
- `DisplayByFileName` → `Path.GetFileName(path)`
- `DisplayByFolderName` → parent folder name only
- `DisplayByFolderFileName` → `folder\file`
- else → full path

Append `GetPauseString()` when paused: ` - paused ………` (dot count = `pauseCountDown / 2`).

---

## Multi-Monitor

`App.xaml.cs` iterates `Screen.AllScreens`. Primary screen gets `MainWindow`; all others get a plain `Blackout` window (black, borderless, maximized).

---

## Known Quirks / Watch-Outs

- **Preview mode**: `MainWindow` is embedded via `HwndSource` into the Windows screensaver preview pane; `RootContainer` Grid is used as `HwndSource.RootVisual` (not the Window itself). Video is excluded in preview mode.
- **`_videoIsActive` flag**: Guards all transition completion callbacks. Must be checked inside every async/animation `Completed` handler to avoid acting on stale state after rapid navigation.
- **`TimerChangeSlide` stopped during video**: Video slides stop `TimerChangeSlide` and `TimerResumeAfterPause`. The timer restarts only in the video→image `fadeOut.Completed` callback or after a clip-timer advance.
- **`TogglePlay` during video**: Pauses/resumes the video rather than the slide timer. The slide show's pause/resume countdown does not apply.
- **`InputBlockingOverlay`**: A transparent `Grid` that sits above `VideoPlayer` in the Z-order. It has its own `MouseDown="Window_MouseDown"` handler so clicks still exit the screensaver. Collapsed in interactive mode.
- **Clip timer vs `MediaEnded`**: Both can trigger `ShowSlide()`. `StopClipTimer()` at the top of `ShowSlide()` prevents a double-advance if `MediaEnded` fires and the clip timer is still pending.
