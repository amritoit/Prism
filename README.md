# Prism ✨

Prism is a native, cross-platform desktop AI chat app built with **Avalonia** and **.NET 10 (C#)**.
It lets you chat with multiple AI providers — **Google Gemini**, **OpenAI (ChatGPT)**, and
**Anthropic (Claude)** — from a single clean, streaming interface, and organize your
conversations into folders.

## Features

- **Multiple providers** — Gemini, OpenAI, and Anthropic behind a common provider interface;
  easy to extend with new ones.
- **Streaming responses** — tokens render live as they arrive (SSE / `IAsyncEnumerable`).
- **Real-time web grounding** — Gemini requests use Google Search grounding for up-to-date
  answers (Gemini-only).
- **Markdown rendering** — headings, bold/italic, inline code, and bullet lists are formatted
  for readable output (custom, dependency-free renderer).
- **Image generation** — supports Gemini image models (text + image responses).
- **Folders & search** — group chats into folders (projects), move chats between folders via a
  per-row menu, and filter by title. The **Recents** list shows your 100 most recent chats.
- **Resizable sidebar** — drag the divider to size the chat list.
- **Persistent history** — settings, chats, and folders are saved locally between sessions.
- **Native single-file build** — publishes to a self-contained `Prism.exe` (no runtime install
  required).

## Tech stack

| Area        | Choice                                                   |
|-------------|----------------------------------------------------------|
| UI          | Avalonia 12 (FluentTheme, compiled bindings)             |
| Runtime     | .NET 10 (`net10.0`, `WinExe`)                            |
| Pattern     | MVVM via CommunityToolkit.Mvvm 8.4 (`[ObservableProperty]`, `[RelayCommand]`) |
| Networking  | `HttpClient` + Server-Sent Events                        |
| Persistence | `System.Text.Json` files                                 |

## Project structure

```
Prism/
├─ Models/        ChatMessage, AppSettings, ChatSession, Project
├─ Providers/     IChatProvider, GeminiProvider, OpenAIProvider, AnthropicProvider, SSE helpers
├─ Services/      SettingsService, SessionStore, ProjectStore
├─ ViewModels/    MainWindowViewModel, SessionViewModel, SessionGroupViewModel, MessageViewModel
├─ Views/         MainWindow (XAML + code-behind)
├─ Controls/      Markdown (attached-property Markdown renderer)
├─ App.axaml(.cs) Composition root / provider registration
└─ Program.cs     Entry point
```

## Configuration & data

Prism stores everything **outside the repository**, under your user profile:

```
%APPDATA%\Prism\
├─ settings.json        API keys, selected provider/model, system prompt
├─ projects.json        Folders
└─ sessions\*.json      Individual chat histories
```

> API keys live only in `%APPDATA%\Prism\settings.json` — they are never written into the
> project folder, so they are not part of Git history. Add your keys in the in-app **Settings**
> dialog. Free Gemini API keys are available from Google AI Studio.

## Getting started

Prerequisites: **.NET 10 SDK**.

Run in development:

```powershell
dotnet run --project Prism.csproj
```

Build (Debug):

```powershell
dotnet build -c Debug
```

Publish a self-contained single-file Windows executable:

```powershell
dotnet publish -c Release -r win-x64 --self-contained true `
  -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true
```

The resulting binary is at:

```
bin\Release\net10.0\win-x64\publish\Prism.exe
```

## Usage

1. Launch Prism and open **Settings** (⚙) to paste your API key(s) and pick a provider/model.
2. Click **+ New chat** (bottom of the sidebar) to start a conversation.
3. Use **⌕** to search chats and **🗂** to create a folder; use a chat's **•••** menu to move it
   into a folder or delete it.
4. Type a message and press **Enter** to send (**Shift+Enter** for a newline).
