# Whisper Prompts Configuration Schema

## Overview
The `prompts.json` file controls the context passed to the Whisper AI model. This is crucial for improving recognition accuracy for specific domains like VTuber streams, gaming, and anime culture.

## File Structure

The JSON structure is keyed by **Language Code**.
Supported keys include full IETF language tags (e.g., `en-US`, `ja-JP`) or two-letter ISO codes (e.g., `en`, `ja`).

### Supported Language Codes (Whisper)
Common codes you can use as keys:
*   `en`, `en-US`, `en-GB`: English
*   `ja`, `ja-JP`: Japanese
*   `zh`, `zh-CN`: Chinese
*   `ko`, `ko-KR`: Korean
*   `es`: Spanish
*   `fr`: French
*   `de`: German
*   `ru`: Russian
*   ...and any other code [supported by Whisper](https://github.com/openai/whisper#available-models-and-languages).

## Configuration Schema

```json
{
  "LANGUAGE_CODE": {
    "meta": {
      "displayName": "Human Readable Name",
      "whisperLanguage": "ISO-639-1 Code (e.g. 'en', 'ja')"
    },
    "scenes": {
      "default": {
        "system": [
          "Instruction 1",
          "Instruction 2"
        ],
        "user": [
          "Optional additional context"
        ],
        "hotwords": {
          "title": "Category Name",
          "items": [
            "Word1",
            "Word2",
            "Complex Term"
          ]
        }
      }
    }
  }
}
```

## Default Configuration (Reference)

### Japanese (ja)
*   **System**: Instructs to remove fillers and prioritize net slang/gaming terms.
*   **Hotwords**: Includes massive list of Hololive/VSPO member names and streaming terms.

### English (en-US)
*   **System**:
    *   "This is a transcript of a VTuber live stream."
    *   "Expect gaming terminology, internet slang, and casual speech."
    *   "Ignore stuttering and filler words."
*   **Hotwords**:
    *   **Streaming**: Super Chat, Membership, Collab, etc.
    *   **Groups**: hololive, vspo, nijisanji.
    *   **Talents**: Full list of Hololive EN/JP talents (e.g., Gura, Calliope, Pekora).
    *   **Games**: Apex, Valorant, Minecraft, etc.

### Simplified Chinese (zh-CN)
*   **System**: Focus on gaming/internet slang in Simplified Chinese.
*   **Hotwords**: Common Bilibili/streaming terms (SC, 舰长, 一键三连) and game names.
