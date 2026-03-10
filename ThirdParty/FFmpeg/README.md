Place the bundled Windows x64 FFmpeg files in this folder as:

- `ffmpeg.exe`
- `LICENSE.txt`
- `README.txt`

PasteIt resolves FFmpeg in this order:

1. User-configured Settings path
2. Bundled `ffmpeg.exe` next to the app binaries
3. `ffmpeg` on `PATH`

The installer expects `ThirdParty/FFmpeg/ffmpeg.exe` to exist and will copy it into the app install directory.
