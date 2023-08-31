# CutOverlay
 CUT Overlay is an open-source OBS overlay with support for Spotify and BeatSaberPlus status.

![Screenshot001](https://github.com/Marakusa/CutOverlay/assets/29477753/674a9a52-6af7-40d5-bbe1-52c5765bb9ac)

## Features
- ✔️ Multiple overlays for different uses (starting soon, chat only, etc.)
- 💬 Twitch chat overlay
  - 7TV Integration
  - BetterTTv Integration (TODO)
  - FrankerFaceZ Integration (TODO)
- 🎧 Real time Spotify status
  - 🎨 Dynamic color theme from album cover art
- ❤️ Pulsoid heart rate integration
- ⚔️ BeatSaberPlus status
- 🕰️ Clock with timezone detection

## Building from the source
Go to the project folder (.csproj)
```
cd .\CutOverlay\
```
Run the following command
```
electronize build /target win /PublishSingleFile false /PublishReadyToRun false
```
The built application is located in `.\bin\Desktop\win-unpacked\CUT Overlay.exe`


![Screenshot002](https://github.com/Marakusa/CutOverlay/assets/29477753/70ee4c7a-ab9c-458b-962c-cdb8ed28a10d)

