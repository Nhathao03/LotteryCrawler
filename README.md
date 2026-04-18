# LotteryCrawler

A .NET 9 API that integrates with multiple AI providers (Claude and OpenAI) with both Web API and CLI interfaces.

## Features

- Dual provider support: Claude (Anthropic) and OpenAI
- CLI mode for quick prompts
- Web API endpoints for integration
- Configurable via settings or environment variables
- Structured logging with Serilog

## Configuration

### Provider Settings

Configure in `appsettings.json`:

```json
{
  "AIProvider": "Claude",  // or "OpenAI"

  "Claude": {
    "ApiKey": "YOUR_CLAUDE_API_KEY",
    "Version": "claude-instant-1",
    "BaseUrl": "https://api.anthropic.com",
    "RequestPath": "/v1/messages",
    "TimeoutSeconds": 30
  },

  "OpenAI": {
    "ApiKey": "YOUR_OPENAI_API_KEY",
    "Model": "gpt-4-1106-preview",
    "BaseUrl": "https://api.openai.com",
    "RequestPath": "/v1/chat/completions",
    "TimeoutSeconds": 30,
    "Temperature": 0.7,
    "MaxTokens": 800
  }
}
```

### Environment Variables

Alternative to settings in appsettings.json:
- Claude: Set `CLAUDE_API_KEY` or `ANTHROPIC_API_KEY`
- OpenAI: Set `OPENAI_API_KEY`

## Usage

### CLI Mode

Run with specific provider:
```bash
# Use OpenAI
dotnet run -- --cli --provider openai --prompt "Your prompt here"

# Use Claude
dotnet run -- --cli --provider claude --prompt "Your prompt here"
```

Interactive mode (uses configured default provider):
```bash
dotnet run -- --cli
# Then enter your prompt when prompted
```

### Web API

Endpoint:
- POST /api/claude/send
  ```json
  {
    "prompt": "your prompt here"
  }
  ```
  Uses the configured default provider from `AIProvider` setting

## Build and Run

```bash
# Restore packages
dotnet restore

# Build
dotnet build

# Run (Web API mode)
dotnet run

# Run (CLI mode)
dotnet run -- --cli
```

## Notes

- The default provider is set by `AIProvider` in appsettings.json
- CLI flag `--provider` overrides the default provider
- Each service has configurable timeout, request paths, and provider-specific options
- Responses are parsed according to each provider's API format
